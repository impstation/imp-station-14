using Content.Server.AlertLevel;
using Content.Server.Announcements.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
///     Manages <see cref="CascadeRuleComponent"/>
/// </summary>
public sealed class CascadeRuleSystem : GameRuleSystem<CascadeRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, CascadeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        SetAlertLevelDelta();

        _announcer.SendAnnouncementMessage(
            _announcer.GetAnnouncementId("NukeArm"),
            "supermatter-component-announcement-resonance-cascade",
            Loc.GetString("supermatter-component-cascade-announcement-sender"),
            Color.Cyan,
            station: _station.GetOwningStation(uid)
        );

        RandomSpawnCrystalMass();
    }

    protected override void ActiveTick(EntityUid uid, CascadeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.TimeUntilEndRound > 0)
        {
            component.TimeUntilEndRound -= frameTime;
            return;
        }

        _roundEndSystem.EndRound();
    }

    // Shuttles called before the full delamination will still arrive, but delamination & round end should be faster than the shuttle arriving if called due to cascade
    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        ev.Cancelled = true;
        ev.Reason = Loc.GetString("resonance-cascade-shuttle-call-unavailable");
    }

    private void RandomSpawnCrystalMass()
    {
        var randomSpawns = _random.Next(1, 4);

        for (var i = 0; i < randomSpawns; i++)
        {
            if (TryFindRandomTile(out _, out _, out var targetGrid, out var coords))
            {
                if (targetGrid is { })
                    continue;

                _map.SetTile(targetGrid, coords, new Tile(_tileDefManager["PlatingCrystalMass"].TileId, 0, (byte)_random.Next(0, 5)));
                Spawn("CrystalMass", coords);
            }
        }
    }

    private void SetAlertLevelDelta()
    {
        if (!TryGetRandomStation(out var station))
            return;
        if (_alertLevelSystem.GetLevel(station.Value) != "delta") // Don't delta if already delta
            return;
        _alertLevelSystem.SetLevel(station.Value, "delta", true, true, true);
    }
}
