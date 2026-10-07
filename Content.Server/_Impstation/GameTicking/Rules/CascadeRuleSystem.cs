using Content.Shared._Impstation.Supermatter.Components;
using Content.Server.AlertLevel;
using Content.Server.Announcements.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Player;

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
    private static readonly string CommandAnnouncementId = "commandReport";
    private static readonly string ShuttlennouncementId = "ShuttleRecalled";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, CascadeRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        comp.TimeUntilInitialAnnouncements = comp.DurationForInitialAnnouncements + Timing.CurTime;
        comp.TimeUntilEndRound = comp.DurationToRoundEnd + Timing.CurTime;

        SetAlertLevelDelta();

        var tile = new Tile(_tileDefManager[comp.CrystalMassPlating].TileId);
        var query = EntityQueryEnumerator<SupermatterComponent>();
        while (query.MoveNext(out var supermatterUid, out var sm))
        {
            if (sm.PreferredDelamType == DelamType.Cascade && sm.DelamEndTime <= Timing.CurTime)
            {
                SpawnSupermatterCrystalMass(supermatterUid, comp, tile);
                EntityManager.QueueDeleteEntity(supermatterUid);
                break;
            }
        }

        // Even if a supermatter is not found, will spawn crystal mass
        // Admins surely won't accidently add this gamerule right...
        SpawnRandomCrystalMass(comp, tile);
    }

    protected override void ActiveTick(EntityUid uid, CascadeRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        if (comp.Stage < ResonanceCascadeStage.BeginningNTAnnouncement
            && comp.TimeUntilInitialAnnouncements - Timing.CurTime < comp.DurationForInitialAnnouncements / 2)
        {
            _announcer.SendAnnouncement(
                _announcer.GetAnnouncementId(CommandAnnouncementId),
                Filter.Broadcast(),
                "resonance-cascade-announcement-begin",
                Loc.GetString("resonance-cascade-announcement-sender"),
                colorOverride: Color.Cyan
            );

            comp.Stage = ResonanceCascadeStage.BeginningNTAnnouncement;
        }

        if (comp.Stage < ResonanceCascadeStage.BeginningEvacCancel
            && comp.TimeUntilInitialAnnouncements < Timing.CurTime)
        {
            if (_roundEndSystem.IsRoundEndRequested())
            {
                _roundEndSystem.CancelRoundEndCountdown(uid, true);
                _announcer.SendAnnouncementMessage(
                    _announcer.GetAnnouncementId(ShuttlennouncementId),
                    "emergancy-shuttle-cascade-enroute",
                    Loc.GetString("emergancy-shuttle-announcement-sender"),
                    Color.Yellow
                );
            }

            comp.Stage = ResonanceCascadeStage.BeginningEvacCancel;
        }

        if (comp.Stage < ResonanceCascadeStage.MiddleNTAnnouncement
            && comp.TimeUntilEndRound - Timing.CurTime < comp.DurationToRoundEnd / 2)
        {
            _announcer.SendAnnouncement(
                _announcer.GetAnnouncementId(CommandAnnouncementId),
                Filter.Broadcast(),
                "resonance-cascade-announcement-middle",
                Loc.GetString("resonance-cascade-announcement-sender"),
                colorOverride: Color.Cyan
            );

            comp.Stage = ResonanceCascadeStage.MiddleNTAnnouncement;
        }

        if (comp.Stage < ResonanceCascadeStage.End
            && comp.TimeUntilEndRound < Timing.CurTime)
        {
            _roundEndSystem.EndRound();

            var count = comp.MinMaxSinglarity.Next(_robustRandom);
            for (var i = 0; i < count; i++)
                if (TryFindRandomTile(out _, out _, out _, out var coords))
                    Spawn(comp.SingularityPrototype, coords);

            comp.Stage = ResonanceCascadeStage.End;
        }
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var cascadeRule, out _))
        {
            if (cascadeRule != null)
            {
                ev.Cancelled = true;
                ev.Reason = Loc.GetString("emergancy-shuttle-cascade-call-unavailable");
            }
        }
    }

    private void SpawnSupermatterCrystalMass(EntityUid supermatterUid, CascadeRuleComponent comp, Tile tile)
    {
        var xform = Transform(supermatterUid);
        var gridUid = xform.GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var supermatterMapGrid))
            return;

        _map.SetTile(gridUid.Value, supermatterMapGrid, xform.Coordinates, tile);
        Spawn(comp.CrystalBulbPrototype, xform.Coordinates);
    }

    private void SpawnRandomCrystalMass(CascadeRuleComponent comp, Tile tile)
    {
        var count = comp.MinMaxCrystalMassSpawn.Next(_robustRandom);
        for (var i = 0; i < count; i++)
        {
            if (TryFindRandomTile(out _, out _, out var targetGrid, out var coords))
            {
                if (!TryComp<MapGridComponent>(targetGrid, out var mapGrid))
                    break;

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
