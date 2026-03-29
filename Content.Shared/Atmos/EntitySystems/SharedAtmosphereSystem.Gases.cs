using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Prototypes;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /*
     Partial class for operations involving GasMixtures.

     Sometimes methods here are abstract because they need different client/server implementations
     due to sandboxing.
     */

    /// <summary>
    /// Cached array of gas specific heats.
    /// </summary>
    public float[] GasSpecificHeats => _gasSpecificHeats;
    private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

    public string?[] GasReagents = new string[Atmospherics.TotalNumberOfGases];
    protected readonly GasPrototype[] GasPrototypes = new GasPrototype[Atmospherics.TotalNumberOfGases];

    public virtual void InitializeGases()
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            var idx = (int)gas;
            // Log an error if the corresponding prototype isn't found
            if (!_prototypeManager.TryIndex<GasPrototype>(gas.ToString(), out var gasPrototype))
            {
                Log.Error($"Failed to find corresponding {nameof(GasPrototype)} for gas ID {(int)gas} ({gas}) with expected ID \"{gas.ToString()}\". Is your prototype named correctly?");
                continue;
            }
            GasPrototypes[idx] = gasPrototype;
            GasReagents[idx] = gasPrototype.Reagent;
        }

        Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat / HeatScale;
        }
    }

    /// <summary>
    ///     Calculates the heat capacity for a gas mixture.
    /// </summary>
    /// <param name="mixture">The mixture whose heat capacity should be calculated</param>
    /// <param name="applyScaling"> Whether the internal heat capacity scaling should be applied. This should not be
    /// used outside of atmospheric related heat transfer.</param>
    /// <returns></returns>
    public float GetHeatCapacity(GasMixture mixture, bool applyScaling)
    {
        var scale = GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);

        // By default GetHeatCapacityCalculation() has the heat-scale divisor pre-applied.
        // So if we want the un-scaled heat capacity, we have to multiply by the scale.
        return applyScaling ? scale : scale * HeatScale;
    }

    protected float GetHeatCapacity(GasMixture mixture)
    {
        return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
    }

    /// <summary>
    /// Gets the heat capacity for a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="moles">The moles array of the <see cref="GasMixture"/></param>
    /// <param name="space">Whether this <see cref="GasMixture"/> represents space,
    /// and thus experiences space-specific mechanics (we cheat and make it a bit cooler).</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract float GetHeatCapacityCalculation(float[] moles, bool space);

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
        NumericsHelpers.Multiply(temp, GasSpecificHeats, temp);
        return NumericsHelpers.HorizontalAdd(temp);
    }
    // END IMP ADD
}
