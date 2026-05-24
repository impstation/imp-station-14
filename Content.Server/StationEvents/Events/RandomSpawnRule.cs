using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
// Imp start
using Content.Server.Announcements.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
// Imp end

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    // Imp start
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    // Remove this when announcement system allows for specified announcements instead of deriving them from the event name
    // Or when the EE announcer system is refactored to support delays
    protected override void Added(EntityUid uid, RandomSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (component.Announcement == null)
            return;

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            component.Announcement,
            colorOverride: Color.Gold);
    }
    // Imp end

    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // Imp start, added MinMax & ability to check tiles for entities before spawning
        var attempt = 0;
        var total = comp.MinMaxEntities.Next(_random);
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
            Spawn(comp.Prototype, coords);
            // Imp TODO: make effects follow the entity moving
            Spawn(comp.SpawnEffect, coords); // Imp, added effects that do not follow the entity around
        }
    }
}
