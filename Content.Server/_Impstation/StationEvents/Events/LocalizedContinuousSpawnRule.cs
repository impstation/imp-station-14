using System.Linq;
using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Events;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class LocalizedContinuousSpawnRule : StationEventSystem<LocalizedContinuousSpawnRuleComponent>
{
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    protected override void Added(EntityUid uid, LocalizedContinuousSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryFindRandomTile(out var tile, out var station, out var grid, out var coords)
            || !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        base.Added(uid, component, gameRule, args);

        component.AvailableTiles = _map.GetLocalTilesIntersecting(
            grid,
            gridComp,
            new Circle(coords.Position, component.Radius.Next(RobustRandom))
            ).ToList();

        component.NearestNavBeacon = _navMap.GetNearestBeaconString(_xform.ToMapCoordinates(coords), true);
        if (component.NearestNavBeaconAnnouncement == null)
            return;

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            component.NearestNavBeaconAnnouncement,
            colorOverride: Color.Gold,
            localeArgs: ("beacon", component.NearestNavBeacon)
            );
    }

    protected override void ActiveTick(EntityUid uid, LocalizedContinuousSpawnRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.TimeUntilNextSpawn)
            return;

        var total = component.Amount.Next(RobustRandom);
        for (var i = 0; i < total; i++)
        {
            var randomTile = RobustRandom.Pick(component.AvailableTiles);
            var coords = _map.ToCenterCoordinates(randomTile);
            foreach (var proto in _entityTable.GetSpawns(component.Table))
            {
                Sawmill.Info($"Spawning {proto} at {coords}");
                Spawn(proto, coords);
            }
        }

        component.TimeUntilNextSpawn = Timing.CurTime + TimeSpan.FromSeconds(component.TimeBetweenSpawn.Next(RobustRandom));
    }

    protected override void Ended(EntityUid uid, LocalizedContinuousSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.NearestNavBeaconEndAnnouncement == null || component.NearestNavBeacon == null)
            return;

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId, true),
            Filter.Broadcast(),
            component.NearestNavBeaconEndAnnouncement,
            colorOverride: Color.Gold,
            localeArgs: ("beacon", component.NearestNavBeacon)
            );
    }
}
