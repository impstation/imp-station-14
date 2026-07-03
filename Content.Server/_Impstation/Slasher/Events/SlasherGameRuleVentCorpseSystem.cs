using Content.Server._Impstation.SpawnCrewCorpses;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Spawns configured crew corpses at eligible vent coordinates on a random station.
/// </summary>
public sealed class SlasherGameRuleVentCorpseSystem : SlasherPulseGameRuleSystem<SlasherGameRuleVentCorpseComponent>
{
    [Dependency] private readonly SpawnCrewCorpseSystem _spawnCrewCorpse = default!;
    [Dependency] private readonly StationSystem _station = default!;
    private readonly ISawmill _sawmill = Logger.GetSawmill("slasher.ventcorpses");

    /// <summary>
    /// Collects vent locations for the selected station and delegates corpse spawning.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleVentCorpseComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<SpawnCrewCorpseComponent>(uid, out var spawnComp))
        {
            _sawmill.Error($"{ToPrettyString(uid)} is missing {nameof(SpawnCrewCorpseComponent)} required by {nameof(SlasherGameRuleVentCorpseSystem)}.");
            return;
        }

        if (!TryGetPulseStation(out var chosenStation))
        {
            _sawmill.Warning($"{ToPrettyString(uid)} could not resolve a Slasher pulse station; skipping vent corpse pulse.");
            return;
        }

        if (chosenStation == null)
            return;

        var vents = new List<EntityCoordinates>();
        var ventsQuery = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (ventsQuery.MoveNext(out var ventUid, out _, out var xform))
        {
            // Resolve station ownership from the vent entity itself; grid station-membership
            // can be absent/transient and caused pulses to silently collect zero candidates.
            if (_station.GetOwningStation(ventUid) != chosenStation.Value)
                continue;

            vents.Add(xform.Coordinates);
        }

        if (vents.Count == 0)
        {
            _sawmill.Warning($"{ToPrettyString(uid)} found no vents on station {ToPrettyString(chosenStation.Value)}; skipping vent corpse pulse.");
            return;
        }

        _spawnCrewCorpse.SpawnCrewCorpses(vents, spawnComp);
    }
}
