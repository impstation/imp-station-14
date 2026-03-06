using Content.Server.AlertLevel;
using Content.Server.AlertLevel.Commands;
using Content.Server.Announcements.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
///     Manages <see cref="CascadeRuleComponent"/>
/// </summary>
public sealed class CascadeRuleSystem : GameRuleSystem<CascadeRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    // [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!; 

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, CascadeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        SetAlertLevelDelta();

        // _announcer.SendAnnouncementMessage(
        //     _announcer.GetAnnouncementId("NukeArm"),
        //     "nuke-component-announcement-armed",
        //     Loc.GetString("nuke-component-announcement-sender"),
        //     Color.Red,
        //     stationUid ?? uid,
        //     null,
        //     null, //imp
        //     ("time", (int)component.RemainingTime),
        //     ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, nukeXform))))
        // );

        // Ends game after rule initialized
        Timer.Spawn(TimeSpan.FromMinutes(2), () => _roundEndSystem.EndRound());
    }

    private void SetAlertLevelDelta()
    {
        if (!TryGetRandomStation(out var chosenStation))
            return;
        if (_alertLevelSystem.GetLevel(chosenStation.Value) != "delta") // Don't delta if already delta
            return;
        _alertLevelSystem.SetLevel(chosenStation.Value, "delta", true, true, true);
    }

    // Shuttles called before the full delamination will still arrive, but delamination & round end should be faster than the shuttle arriving if called due to cascade
    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        ev.Cancelled = true;
        ev.Reason = Loc.GetString("resonance-cascade-shuttle-call-unavailable");
    }

}
