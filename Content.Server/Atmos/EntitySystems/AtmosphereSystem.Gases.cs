using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private GasReactionPrototype[] _gasReactions = [];

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions;

        public override void InitializeGases()
        {
            base.InitializeGases();

            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));
        }

        public override float GetMass(GasMixture mix)
        {
            return GetMass(mix.Moles);
        }

        public override float GetMass(float[] moles)
        {
            Span<float> tmp = stackalloc float[moles.Length];
            NumericsHelpers.Multiply(moles, GasMolarMasses, tmp);

            // Conversion of grams to kilograms.
            return NumericsHelpers.HorizontalAdd(tmp) * Atmospherics.gToKg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float GetHeatCapacityCalculation(float[] moles, bool space)
        {
            // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
            if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            Span<float> tmp = stackalloc float[moles.Length];
            NumericsHelpers.Multiply(moles, GasMolarHeatCapacities, tmp);
            // Adjust heat capacity by speedup, because this is primarily what
            // determines how quickly gases heat up/cool.
            return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
        }

        public override bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        {
            Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
            NumericsHelpers.Multiply(mixture.Moles, GasFuelMask, tmp);
            return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
        }

        public override bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        {
            Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
            NumericsHelpers.Multiply(mixture.Moles, GasOxidizerMask, tmp);
            return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
        }

        /// <summary>
        ///     Return speedup factor for pumped or flow-based devices that depend on MaxTransferRate.
        /// </summary>
        public float PumpSpeedup()
        {
            return Speedup;
        }

        /// <summary>
        ///     Add 'dQ' Joules of energy into 'mixture'.
        /// </summary>
        public void AddHeat(GasMixture mixture, float dQ)
        {
            var c = GetHeatCapacity(mixture);
            float dT = dQ / c;
            mixture.Temperature += dT;
        }

        /// <summary>
        ///     Divides a source gas mixture into several recipient mixtures, scaled by their relative volumes. Does not
        ///     modify the source gas mixture. Used for pipe network splitting. Note that the total destination volume
        ///     may be larger or smaller than the source mixture.
        /// </summary>
        public void DivideInto(GasMixture source, List<GasMixture> receivers)
        {
            var totalVolume = 0f;
            foreach (var receiver in receivers)
            {
                if (!receiver.Immutable)
                    totalVolume += receiver.Volume;
            }

            float? sourceHeatCapacity = null;
            var buffer = new float[Atmospherics.AdjustedNumberOfGases];

            foreach (var receiver in receivers)
            {
                if (receiver.Immutable)
                    continue;

                var fraction = receiver.Volume / totalVolume;

                // Set temperature, if necessary.
                if (MathF.Abs(receiver.Temperature - source.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
                {
                    // Often this divides a pipe net into new and completely empty pipe nets
                    if (receiver.TotalMoles == 0)
                        receiver.Temperature = source.Temperature;
                    else
                    {
                        sourceHeatCapacity ??= GetHeatCapacity(source);
                        var receiverHeatCapacity = GetHeatCapacity(receiver);
                        var combinedHeatCapacity = receiverHeatCapacity + sourceHeatCapacity.Value * fraction;
                        if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                            receiver.Temperature = (GetThermalEnergy(source, sourceHeatCapacity.Value * fraction) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                    }
                }

                // transfer moles
                NumericsHelpers.Multiply(source.Moles, fraction, buffer);
                NumericsHelpers.Add(receiver.Moles, buffer);
            }
        }

        /// <summary>
        ///     Releases gas from this mixture to the output mixture.
        ///     If the output mixture is null, then this is being released into space.
        ///     It can't transfer air to a mixture with higher pressure.
        /// </summary>
        public bool ReleaseGasTo(GasMixture mixture, GasMixture? output, float targetPressure)
        {
            var outputStartingPressure = output?.Pressure ?? 0;
            var inputStartingPressure = mixture.Pressure;

            if (outputStartingPressure >= MathF.Min(targetPressure, inputStartingPressure - 10))
                // No need to pump gas if the target is already reached or input pressure is too low.
                // Need at least 10 kPa difference to overcome friction in the mechanism.
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var pressureDelta = MathF.Min(targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure) / 2f);
            var transferMoles = pressureDelta * (output?.Volume ?? Atmospherics.CellVolume) / (mixture.Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = mixture.Remove(transferMoles);

            if(output != null)
                Merge(output, removed);

            return true;
        }

        /// <summary>
        ///     Pump gas from this mixture to the output mixture.
        ///     Amount depends on target pressure.
        /// </summary>
        /// <param name="mixture">The mixture to pump the gas from</param>
        /// <param name="output">The mixture to pump the gas to</param>
        /// <param name="targetPressure">The target pressure to reach</param>
        /// <returns>Whether we could pump air to the output or not</returns>
        public bool PumpGasTo(GasMixture mixture, GasMixture output, float targetPressure)
        {
            var outputStartingPressure = output.Pressure;
            var pressureDelta = targetPressure - outputStartingPressure;

            if (pressureDelta < 0.01)
                // No need to pump gas, we've reached the target.
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var transferMoles = pressureDelta * output.Volume / (mixture.Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = mixture.Remove(transferMoles);
            Merge(output, removed);
            return true;
        }

        /// <summary>
        ///     Scrubs specified gases from a gas mixture into a <see cref="destination"/> gas mixture.
        /// </summary>
        public void ScrubInto(GasMixture mixture, GasMixture destination, IReadOnlyCollection<Gas> filterGases)
        {
            var buffer = new GasMixture(mixture.Volume){Temperature = mixture.Temperature};

            foreach (var gas in filterGases)
            {
                buffer.AdjustMoles(gas, mixture.GetMoles(gas));
                mixture.SetMoles(gas, 0f);
            }

            Merge(destination, buffer);
        }

        /// <summary>
        ///     Checks whether a gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="air">Mixture to be checked.</param>
        /// <returns>Whether the mixture is probably safe.</returns>
        public bool IsMixtureProbablySafe(GasMixture? air)
        {
            // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
            if (air == null)
                return false;

            switch (air.Pressure)
            {
                case <= Atmospherics.WarningLowPressure:
                case >= Atmospherics.WarningHighPressure:
                    return false;
            }

            switch (air.Temperature)
            {
                case <= 260:
                case >= 360:
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Compares two TileAtmospheres to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(TileAtmosphere sample, TileAtmosphere otherSample)
        {
            if (sample.AirArchived == null || otherSample.AirArchived == null)
                return GasCompareResult.NoExchange;

            return CompareExchange(sample.AirArchived, otherSample.AirArchived);
        }

        /// <summary>
        ///     Compares two gas mixtures to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(GasMixture sample, GasMixture otherSample)
        {
            var moles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasMoles = sample.Moles[i];
                var delta = MathF.Abs(gasMoles - otherSample.Moles[i]);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                    return (GasCompareResult)i; // We can move gases!
                moles += gasMoles;
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(sample.Temperature - otherSample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return GasCompareResult.TemperatureExchange; // There can be temperature exchange.
            }

            // No exchange at all!
            return GasCompareResult.NoExchange;
        }

        [PublicAPI]
        public override ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        {
            var reaction = ReactionResult.NoReaction;
            var temperature = mixture.Temperature;
            var energy = GetThermalEnergy(mixture);

            foreach (var prototype in GasReactions)
            {
                if (energy < prototype.MinimumEnergyRequirement ||
                    temperature < prototype.MinimumTemperatureRequirement ||
                    temperature > prototype.MaximumTemperatureRequirement)
                    continue;

                var doReaction = true;
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    var req = prototype.MinimumRequirements[i];

                    if (!(mixture.GetMoles(i) < req))
                        continue;

                    doReaction = false;
                    break;
                }

                if (!doReaction)
                    continue;

                reaction = prototype.React(mixture, holder, this, HeatScale);
                if(reaction.HasFlag(ReactionResult.StopReactions))
                    break;
            }

            return reaction;
        }

        /// <summary>
        /// Adds an array of moles to a <see cref="GasMixture"/>.
        /// Guards against negative moles by clamping to zero.
        /// </summary>
        /// <param name="mixture">The <see cref="GasMixture"/> to add moles to.</param>
        /// <param name="molsToAdd">The <see cref="ReadOnlySpan{T}"/> of moles to add.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the length of the <see cref="ReadOnlySpan{T}"/>
        /// is not the same as the length of the <see cref="GasMixture"/> gas array.</exception>
        [PublicAPI]
        public static void AddMolsToMixture(GasMixture mixture, ReadOnlySpan<float> molsToAdd)
        {
            // Span length should be as long as the length of the gas array.
            // Technically this is a redundant check because NumericsHelpers will do the same thing,
            // but eh.
            ArgumentOutOfRangeException.ThrowIfNotEqual(mixture.Moles.Length, molsToAdd.Length, nameof(mixture.Moles.Length));

            NumericsHelpers.Add(mixture.Moles, molsToAdd);
            NumericsHelpers.Max(mixture.Moles, 0f);
        }

        public enum GasCompareResult
        {
            NoExchange = -2,
            TemperatureExchange = -1,
        }

        // BEGIN IMP ADD
        /// <summary>
        ///     Calculates the specific heat for a gas mixture.
        ///     <br></br><b>Ensure that you know the difference between specific heat and heat capacity, and that you're using the correct function.</b>
        /// </summary>Expand commentComment on lines L43 to R48Resolved
        /// <param name="mixture">The mixture whose specific heat should be calculated</param>
        public float GetSpecificHeat(GasMixture mixture)
            => GetSpecificHeatCalculation(mixture.Moles, mixture.Immutable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetSpecificHeatCalculation(float[] moles, bool space)
        {
            //Mirroring heat capacity calculation: return space's specific heat
            if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            //Create a proportional moles vector. Essentially we want the heat capacity of one mol of gas.
            //Note: we *don't* want a normalized vector, as the denominator of the fraction will be a square root.
            //E.g. A mix with 50/50 oxygen/nitrogen will compute to be 0.70 oxygen and 0.70 nitrogen instead of 0.5 and 0.5.
            Span<float> temp = stackalloc float[moles.Length];

            //Get the sum of the vector.
            var molesSum = NumericsHelpers.HorizontalAdd(moles);

            //This should never happen, but just in case.
            if (molesSum == 0f)
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            //Proportionalize the mols vector by dividing all elements within it by its sum.
            //Here temp contains the fractional values of each gas in the mixture.
            NumericsHelpers.Divide(moles, molesSum, temp);

            //Multiply fractional moles with specific heats and return the sum. This is the specific heat of the mixture.
            //Here temp contains the partial specific heat of each gas in the mixture.
            NumericsHelpers.Multiply(temp, GasMolarHeatCapacities, temp);
            return NumericsHelpers.HorizontalAdd(temp);
        }
        // END IMP ADD
    }
}
