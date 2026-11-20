using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Impstation.Atmos.Reactions;

/// <summary>
/// Chemical Equation: H₄N₂₂O₁₁ + Heat → H₄N₂₂O₁₁⁺
/// This reaction converts electrovae to charged electrovae using thermal energy
/// Higher temperatures increase efficiency (Arrhenius)
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeChargeReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialE = mixture.GetMoles(Gas.Electrovae);
        var efficiency = CalculateHeatBasedEfficiency(mixture.Temperature);

        // Scale the reaction by efficiency and available electrovae
        var chargeAmount = Math.Min(initialE * efficiency * Atmospherics.ElectrovaeChargeChargingRateMultiplier, initialE);

        mixture.AdjustMoles(Gas.Electrovae, -chargeAmount);
        mixture.AdjustMoles(Gas.ChargedElectrovae, chargeAmount);

        // Expose the tile to charged electrovae - AtmosphereSystem will handle the rest
        if (holder is TileAtmosphere tileAtmos)
        {
            var intensity = Math.Min(mixture.GetMoles(Gas.ChargedElectrovae) / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);
            atmosphereSystem.ChargedElectrovaeExpose(tileAtmos.GridIndex, tileAtmos, intensity);
        }

        // IMP TODO: add evil pipe bursting

        // Consume some heat energy during the charging process
        atmosphereSystem.AddHeat(mixture, -chargeAmount * Atmospherics.ElectrovaeChargeEnergyPerMole);

        return ReactionResult.Reacting;
    }

    /// <summary>
    /// Calculate charging efficiency based on temperature
    /// </summary>
    private static float CalculateHeatBasedEfficiency(float temperature)
    {
        // Efficient above 1508K, peaks around 1984K+
        if (temperature >= Atmospherics.ElectrovaeChargeMaxTemperature)
            return 1f;

        var normalized = (temperature - Atmospherics.ElectrovaeChargeMinTemperature) / (Atmospherics.ElectrovaeChargeMaxTemperature - Atmospherics.ElectrovaeChargeMinTemperature);
        return MathF.Pow(normalized, Atmospherics.ElectrovaeChargeTemperatureExponent);
    }
}
