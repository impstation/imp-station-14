using System.Linq;
using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class LocalizedRadiationStormRule : StationEventSystem<LocalizedRadiationStormRuleComponent>
{
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    private static readonly EntProtoId<RadiationPulseComponent> RadiationPulse = "SmallRadiationPulse";

    protected override void Added(EntityUid uid, LocalizedRadiationStormRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryFindRandomTile(out var tile, out var station, out var grid, out var coords))
            return;

        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        base.Added(uid, component, gameRule, args);

        component.AvailableTiles = _map.GetLocalTilesIntersecting(
            grid,
            gridComp,
            new Circle(coords.Position, component.StormRadius.Next(RobustRandom))
            ).ToList();

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            component.Announcement,
            colorOverride: Color.Gold,
            localeArgs: ("beacon", _navMap.GetNearestBeaconString(_xform.ToMapCoordinates(coords), true))
            );
    }

    protected override void ActiveTick(EntityUid uid, LocalizedRadiationStormRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.TimeUntilNextPulse)
            return;

        var randomTile = RobustRandom.Pick(component.AvailableTiles);

        Spawn(RadiationPulse, _map.ToCenterCoordinates(randomTile));

        component.TimeUntilNextPulse = Timing.CurTime + TimeSpan.FromSeconds(component.TimeBetweenPulse.Next(RobustRandom));
    }
}
