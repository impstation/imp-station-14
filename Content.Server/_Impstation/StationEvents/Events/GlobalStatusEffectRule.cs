using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

/// <summary>
/// Applies a given status effect to every humanoid on the station, for a random duration between max and min per-entity.
/// </summary>
public sealed class GlobalStatusEffectRule : StationEventSystem<GlobalStatusEffectRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    protected override void Added(EntityUid uid, GlobalStatusEffectRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station) || component.Announcement == null)
            return;

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            component.Announcement,
            colorOverride: Color.Gold);
    }

    protected override void Started(EntityUid uid, GlobalStatusEffectRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var ent, out var mindComp, out _))
        {
            if (!mindComp.HasMind)
                continue;

            // yes i know this looks bad. its a ternary. in summary, if both min and max aren't null, it returns a random timespan in seconds. otherwise it returns null
            TimeSpan? duration = component.MinEffectDuration != null && component.MaxEffectDuration != null
                        ? TimeSpan.FromSeconds(_random.NextFloat((float)component.MinEffectDuration, (float)component.MaxEffectDuration!))
                        : null;

            _statusEffect.TryUpdateStatusEffectDuration(ent, component.StatusEffect, duration);
        }
    }
}
