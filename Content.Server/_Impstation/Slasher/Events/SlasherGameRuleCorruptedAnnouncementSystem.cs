using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Broadcasts a randomized corrupted station announcement for a Slasher pulse.
/// </summary>
public sealed class SlasherGameRuleCorruptedAnnouncementSystem : GameRuleSystem<SlasherGameRuleCorruptedAnnouncementComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// Picks one configured localization key and sends a global red announcement without stinger audio.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleCorruptedAnnouncementComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.AnnouncementLocaleKeys.Count == 0)
            return;

        var localeKey = component.AnnouncementLocaleKeys[RobustRandom.Next(component.AnnouncementLocaleKeys.Count)];
        var message = Loc.GetString(localeKey);
        var sender = Loc.GetString("slasher-pulse-announcement-sender");

        // No stinger sound — the silence makes it creepier.
        _chat.DispatchGlobalAnnouncement(message, sender: sender, playSound: false, colorOverride: Color.Red);
    }
}
