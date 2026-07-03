using Content.Server.GameTicking.Rules;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Announcements.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Executes a delayed station-wide APC breaker outage, then restores affected APCs when the pulse ends.
/// </summary>
public sealed class SlasherGameRulePowerOutageSystem : SlasherPulseGameRuleSystem<SlasherGameRulePowerOutageComponent>
{
    [Dependency] private readonly ApcSystem _apc = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;

    /// <summary>
    /// Schedules outage timing, picks an affected station, and sends the warning announcement.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRulePowerOutageComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.OutageStartsAt = Timing.CurTime + TimeSpan.FromSeconds(component.ShutdownDelay);
        component.EndsAt = component.OutageStartsAt + TimeSpan.FromSeconds(component.Duration);
        component.OutageStarted = false;
        component.AffectedStation = null;

        if (TryGetPulseStation(out var chosenStation) && chosenStation is { } station)
            component.AffectedStation = station;

        var warningId = _announcer.GetAnnouncementId("PowerGridCheck");
        _announcer.SendAnnouncement(warningId, Filter.Broadcast(), _announcer.GetEventLocaleString(warningId), colorOverride: Color.Gold);
    }

    /// <summary>
    /// Opens APC main breakers on the target station and records affected APCs for restoration.
    /// </summary>
    /// <param name="station">Station entity UID to affect.</param>
    /// <param name="component">Rule runtime state/configuration.</param>
    private void BeginOutage(EntityUid station, SlasherGameRulePowerOutageComponent component)
    {
        // Kill every enabled APC on the station after the warning delay.
        var query = AllEntityQuery<ApcComponent>();
        while (query.MoveNext(out var apcUid, out var apc))
        {
            if (!IsOnPulseStation(apcUid, station))
                continue;

            if (!apc.MainBreakerEnabled)
                continue;

            _apc.ApcToggleBreaker(apcUid, apc);
            component.Unpowered.Add(apcUid);
        }

        component.OutageStarted = true;
    }

    /// <summary>
    /// Starts outage when delay elapses and ends the rule when total duration is reached.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule runtime state/configuration.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="frameTime">Frame delta in seconds.</param>
    protected override void ActiveTick(EntityUid uid, SlasherGameRulePowerOutageComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!component.OutageStarted && Timing.CurTime >= component.OutageStartsAt)
        {
            if (component.AffectedStation is { } station)
                BeginOutage(station, component);
            else
                component.OutageStarted = true;
        }

        if (Timing.CurTime >= component.EndsAt)
            GameTicker.EndGameRule(uid, gameRule);
    }

    /// <summary>
    /// Restores APC breakers that were disabled by this rule and sends completion announcement.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule runtime state/configuration.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule end event data.</param>
    protected override void Ended(EntityUid uid, SlasherGameRulePowerOutageComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        // Restore all APCs that were turned off.
        foreach (var apcUid in component.Unpowered)
        {
            if (!TryComp<ApcComponent>(apcUid, out var apc) || apc.MainBreakerEnabled)
                continue;

            _apc.ApcToggleBreaker(apcUid, apc);
        }

        var completeId = _announcer.GetAnnouncementId("PowerGridCheck", true);
        _announcer.SendAnnouncement(completeId, Filter.Broadcast(), _announcer.GetEventLocaleString(completeId), colorOverride: Color.Gold);
    }
}
