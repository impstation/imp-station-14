using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
// Moffstation - Start - Syndicate dead drop
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Robust.Shared.Utility;
// Moffstation - End

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!; // imp
    // Moffstation - Start - Syndicate dead drop
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    // Moffstation - End

    /// <summary>
    /// Imp edited summary.
    /// Finds a random tile on the station and spawns a entity and its effect if specified.
    /// Conditionally checks for if the tile has another dynamic or static entity on it before spawning.
    /// Conditionally sends a radio message with the sender being the name of the entity spawned. 
    /// </summary>
    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // Imp start, added MinMax & ability to check tiles for entities before spawning
        var attempt = 0;
        var total = comp.MinMaxEntities.Next(RobustRandom);
        for (var i = 0; i < total; i++)
        {
            if (!TryFindRandomTile(out var tileIndices, out _, out var grid, out var coords))
                continue;

            if (comp.EmptyTilesOnly
                && _lookup.GetLocalEntitiesIntersecting(grid, tileIndices, flags: LookupFlags.Dynamic | LookupFlags.Static).Count != 0
                && attempt < 100) // If it fails that much just let it spawn
            {
                attempt++;
                i--;
                continue;
            }
            // Imp end

            Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
            var ent = Spawn(comp.Prototype, coords); // Moff, tied spawned to a ent

            // Moffstation - Syndicate dead drop
            if (comp.RadioMessage is {} radioMessage)
            {
                var message = Loc.GetString(radioMessage, ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent)))); // Imp, changed from radioMessage.Message to radioMessage
                _radio.SendRadioMessage(ent, message, comp.Channel, ent); // Imp, changed from radioMessage.Channel to comp.Channel
            }
            // Moffstation - End
        }
    }
}
