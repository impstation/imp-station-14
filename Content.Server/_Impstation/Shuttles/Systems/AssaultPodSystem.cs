using Content.Server._Impstation.Shuttles.Components;
using Content.Server._Impstation.Shuttles.Events;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Pinpointer;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Shuttles.Systems
{
    public sealed class AssaultPodSystem : EntitySystem
    {
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AssaultPodComponent, AssaultPodStartedEvent>(OnAssaultPodStarted);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<AssaultPodComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.LaunchTime == TimeSpan.Zero || comp.LaunchTime > _timing.CurTime)
                    continue;

                if (!TryComp<ShuttleComponent>(uid, out var shuttle))
                    continue;

                comp.LaunchTime = TimeSpan.Zero;

                _shuttle.FTLToCoordinates(
                    uid,
                    shuttle,
                    comp.TravelCoordinates,
                    Angle.Zero,
                    hyperspaceTime: comp.TravelTime,
                    globalTravelSound: true,
                    destroyFloor: true,
                    arrivalKnockRadius: comp.ArrivalKnockRadius,
                    travelSound: comp.TravelSound,
                    globalArrivalSound: comp.ArrivalSound);

                foreach (var rule in _gameTicker.GetActiveGameRules<NukeopsRuleComponent>())
                {
                    if (rule.Comp.TargetStation is not { } targetStation)
                        continue;

                    _alertLevelSystem.SetLevel(targetStation, comp.SetStationAlertLevel, false, true, true);
                    break;
                }

                var destinationMapCoords = _transform.ToMapCoordinates(comp.TravelCoordinates);
                var beacon = _navMap.GetNearestBeaconString(destinationMapCoords, true);
                _chat.DispatchFilteredAnnouncement(
                    Filter.BroadcastMap(destinationMapCoords.MapId),
                    Loc.GetString(comp.DepartureStationAnnouncement, ("beacon", beacon)),
                    uid,
                    Loc.GetString(comp.StationAnnouncementSender),
                    announcementSound: comp.DepartureAnnouncementSound,
                    colorOverride: Color.Cyan
                );
            }
        }

        private void OnAssaultPodStarted(Entity<AssaultPodComponent> ent, ref AssaultPodStartedEvent args)
        {
            ent.Comp.LaunchTime = ent.Comp.TimeTillLaunch + _timing.CurTime;
            ent.Comp.TravelCoordinates = args.Coords;
        }
    }
}
