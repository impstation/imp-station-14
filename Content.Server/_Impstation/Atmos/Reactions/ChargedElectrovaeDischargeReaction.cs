using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Impstation.Atmos.Reactions;

/// <summary>
/// Marks tiles with charged electrovae for discharge effects.
/// The actual effects (power effects and lightning strikes) are handled by AtmosphereSystem.
/// </summary>
[UsedImplicitly]
public sealed partial class ChargedElectrovaeDischargeReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialCE = mixture.GetMoles(Gas.ChargedElectrovae);

        if (holder is TileAtmosphere tileAtmos)
        {
            var intensity = Math.Min(initialCE / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);
            atmosphereSystem.ChargedElectrovaeExpose(tileAtmos.GridIndex, tileAtmos, intensity);
        }

        // IMP TODO: add pipe damaging if reacted not in a tile (evil pipe bursting)

        // Slowly discharge via exponential decay: ChargedElectrovae -> Electrovae
        var decayAmount = initialCE * Atmospherics.ChargedElectrovaeDecayConstant;
        mixture.AdjustMoles(Gas.ChargedElectrovae, -decayAmount);
        mixture.AdjustMoles(Gas.Electrovae, decayAmount);

        return ReactionResult.Reacting;
    }
}
