using System.Linq;
using Content.Server._Impstation.Shuttles.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared._Impstation.Shuttles.Events;
using Content.Shared.Lock;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Shuttles.Systems
{
    public sealed class AssaultPodConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly LockSystem _lockSystem = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly MapSystem _mapSystem = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AssaultPodPurchasedEvent>(OnAssaultPodPurchased);
            Subs.BuiEvents<AssaultPodConsoleComponent>(StationMapUiKey.Key, subs =>
            {
                subs.Event<ClickCoordMessage>(OnClickCoord);
            });
        }

        private void OnAssaultPodPurchased(AssaultPodPurchasedEvent args)
        {
            var query = EntityQueryEnumerator<AssaultPodConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                comp.Purchased = true;
                _lockSystem.Unlock(uid, null);
            }
        }

        private void OnClickCoord(Entity<AssaultPodConsoleComponent> ent, ref ClickCoordMessage args)
        {
            if (!ent.Comp.Purchased || ent.Comp.Started)
                return;

            var xform = Transform(ent);
            if (xform.GridUid is not { } gridUid)
                return;

            var landingCoords = GetLandingCoords(args.Coordinates, ent.Comp.LandingVariationRange);
            if (landingCoords == EntityCoordinates.Invalid)
                return;

            ent.Comp.Started = true;

            var ev = new AssaultPodStartedEvent(landingCoords);
            RaiseLocalEvent(gridUid, ref ev);

            var beacon = _navMap.GetNearestBeaconString(_transform.ToMapCoordinates(landingCoords), true);
            _chat.DispatchFilteredAnnouncement(
                Filter.BroadcastMap(xform.MapID),
                Loc.GetString(ent.Comp.BeginDepartureTimerAnnouncement, ("beacon", beacon)),
                sender: Loc.GetString(ent.Comp.NukieAnnouncementSender),
                announcementSound: ent.Comp.BeginDepartureTimerAnnouncementSound,
                colorOverride: Color.DarkRed
            );
        }

        private EntityCoordinates GetLandingCoords(MapCoordinates coords, float variationRadius)
        {
            if (!_mapMan.TryFindGridAt(coords.MapId, coords.Position, out var grid, out var gridComp))
                return EntityCoordinates.Invalid;

            if (!IsNukeOpsTargetLargestGrid(grid))
                return EntityCoordinates.Invalid;

            var tileRefs = _mapSystem.GetTilesIntersecting(
                grid,
                gridComp,
                new Circle(coords.Position, variationRadius)
                ).ToList();

            if (tileRefs.Count == 0)
                return EntityCoordinates.Invalid;

            var chosenTile = _random.Pick(tileRefs);
            var gridEntityCoords = _mapSystem.ToCoordinates(chosenTile, gridComp);
            // conversion to prevent dock ftl from triggering
            return _transform.WithEntityId(gridEntityCoords, _mapSystem.GetMap(coords.MapId));
        }

        private bool IsNukeOpsTargetLargestGrid(EntityUid grid)
        {
            foreach (var rule in _gameTicker.GetActiveGameRules<NukeopsRuleComponent>())
            {
                if (rule.Comp.TargetStation is not { } targetStation)
                    continue;

                if (_station.GetLargestGrid(targetStation) == grid)
                    return true;
            }

            return false;
        }
    }
}
