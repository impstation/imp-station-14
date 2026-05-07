using Content.Shared._EE.Supermatter.Components;
using Content.Server.AlertLevel;
using Content.Server.Announcements.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
///     Manages <see cref="CascadeRuleComponent"/>
/// </summary>
public sealed class CascadeRuleSystem : GameRuleSystem<CascadeRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, CascadeRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        SetAlertLevelDelta();

        _announcer.SendAnnouncementMessage(
            _announcer.GetAnnouncementId("NukeArm"),
            "resonance-cascade-announcement-begin",
            Loc.GetString("resonance-cascade-announcement-sender"),
            Color.Cyan,
            station: _station.GetOwningStation(uid)
        );

        var query = EntityQueryEnumerator<SupermatterComponent>();
        while (query.MoveNext(out var supermatterUid, out var sm))
        {
            // Testing
            // if (sm.PreferredDelamType == DelamType.Cascade && sm.DelamEndTime <= Timing.CurTime)
            // {
            // Two will not be fully delaminating at the exact same time... right?
            SpawnCrystalMass(supermatterUid, comp);
            EntityManager.QueueDeleteEntity(supermatterUid);
            break;
            // }
        }
    }

    protected override void ActiveTick(EntityUid uid, CascadeRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime > comp.TimeUntilEndRound / 2 && comp.Stage < ResonanceCascadeStage.Middle)
        {
            _announcer.SendAnnouncementMessage(
                _announcer.GetAnnouncementId("NukeArm"),
                "resonance-cascade-announcement-middle",
                Loc.GetString("resonance-cascade-announcement-sender"),
                Color.Cyan,
                station: _station.GetOwningStation(uid)
            );

            comp.Stage = ResonanceCascadeStage.Middle;
        }

        if (Timing.CurTime > comp.TimeUntilEndRound && comp.Stage < ResonanceCascadeStage.End)
        {
            _roundEndSystem.EndRound();

            for (var i = 0; i < 3; i++)
            {
                if (TryFindRandomTile(out _, out _, out _, out var coords))
                {
                    Spawn(comp.SingularityPrototype, coords);
                }
            }

            comp.Stage = ResonanceCascadeStage.End;
        }
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        ev.Cancelled = true;
        ev.Reason = Loc.GetString("emergancy-shuttle-cascade-call-unavailable");
    }

    private void SpawnCrystalMass(EntityUid supermatterUid, CascadeRuleComponent comp)
    {
        var xform = Transform(supermatterUid);
        var gridUid = xform.GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var tile = new Tile(_tileDefManager[comp.CrystalMassPlating].TileId);

        _map.SetTile(gridUid.Value, mapGrid, xform.Coordinates, tile);
        Spawn(comp.CrystalBulbPrototype, xform.Coordinates);

        var randomSpawns = _robustRandom.Next(1, 4);

        for (var i = 0; i < randomSpawns; i++)
        {
            if (TryFindRandomTile(out _, out _, out var targetGrid, out var coords))
            {
                // For if a supermatter offgrid cascade delams
                if (targetGrid != gridUid)
                    return;

                _map.SetTile(targetGrid, mapGrid, coords, tile);
                Spawn(comp.CrystalBulbPrototype, coords);
            }
        }
    }

    private void SetAlertLevelDelta()
    {
        if (!TryGetRandomStation(out var station))
            return;
        if (_alertLevelSystem.GetLevel(station.Value) == "delta") // Don't delta if already delta
            return;
        _alertLevelSystem.SetLevel(station.Value, "delta", true, true, true);
    }
}
