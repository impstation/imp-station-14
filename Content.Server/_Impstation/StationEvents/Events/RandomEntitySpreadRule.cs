using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

/// <summary>
/// Spawns a random amount of specified entites throughout the station, ignoring tiles where there are mobs
/// </summary>
public sealed class RandomEntitySpreadRule : StationEventSystem<RandomEntitySpreadRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    protected override void Added(EntityUid uid, RandomEntitySpreadRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
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

    protected override void Started(EntityUid uid, RandomEntitySpreadRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var total = component.MinMaxEntities.Next(_random);
        for (var i = 0; i < total; i++)
        {
            if (!TryFindRandomTile(out _, out _, out var grid, out var coords))
                continue;

            if (!TryComp<MapGridComponent>(grid, out var map))
                return;

            var tile = _mapSystem.GetTileRef(grid, map, coords);

            // Ignore tiles with mobs or machines
            var entities = _lookup.GetEntitiesInTile(tile, LookupFlags.Dynamic | LookupFlags.Static);
            if (entities.Count != 0)
            {
                i--;
                continue;
            }

            Spawn(component.SpawnEffect, coords);
            Spawn(component.SpawnedEntity, coords);
        }
    }
}
