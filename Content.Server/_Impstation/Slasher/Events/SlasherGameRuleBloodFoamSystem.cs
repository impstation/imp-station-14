using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Triggers blood-foam emissions from a random subset of station vents during a Slasher pulse.
/// </summary>
public sealed class SlasherGameRuleBloodFoamSystem : SlasherPulseGameRuleSystem<SlasherGameRuleBloodFoamComponent>
{
    [Dependency] private readonly SmokeSystem _smoke = default!;

    /// <summary>
    /// Selects a station, iterates its vents, and emits foam with configured reagent mixes.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleBloodFoamComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetPulseStation(out var chosenStation))
            return;

        // Sparse vent procs keep this creepy without flooding every corridor.
        foreach (var (_, xform) in EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            if (RobustRandom.NextFloat() > component.VentProcChance)
                continue;

            var solution = new Solution();
            var reagent = component.ReagentId;
            if (component.BloodReagentWhitelist.Count > 0)
                reagent = component.BloodReagentWhitelist[RobustRandom.Next(component.BloodReagentWhitelist.Count)];

            solution.AddReagent(reagent, component.ReagentQuantity);

            var foamEnt = Spawn(ChemicalReactionSystem.FoamReaction, xform.Coordinates);
            _smoke.StartSmoke(foamEnt, solution, component.FoamTime, component.Spread);
        }
    }
}
