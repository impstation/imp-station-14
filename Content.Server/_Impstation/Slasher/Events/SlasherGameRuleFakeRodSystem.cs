using Content.Server.Announcements.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Emits a fake immovable-rod announcement as a Slasher pulse scare event.
/// </summary>
public sealed class SlasherGameRuleFakeRodSystem : GameRuleSystem<SlasherGameRuleFakeRodComponent>
{
    [Dependency] private readonly AnnouncerSystem _announcer = default!;

    /// <summary>
    /// Sends the immovable-rod style announcement to all players.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleFakeRodComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var announcementId = _announcer.GetAnnouncementId("ImmovableRodSpawn");
        var locale = _announcer.GetEventLocaleString(announcementId);

        _announcer.SendAnnouncement(announcementId, Filter.Broadcast(), locale, colorOverride: Color.Gold);
    }
}
