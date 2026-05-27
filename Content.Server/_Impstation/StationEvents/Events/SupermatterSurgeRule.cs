using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.Lightning;
using Content.Server.StationEvents.Events;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class SupermatterSurgeRule : StationEventSystem<SupermatterSurgeRuleComponent>
{
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Finding a active supermatter for the event and sending an announcement before the event starts.
    /// </summary>
    protected override void Added(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var supermatterUids = new List<EntityUid>();
        var query = EntityQueryEnumerator<SupermatterComponent>();

        while (query.MoveNext(out var supermatterUid, out var sm))
        {
            // Does not target inactive supermatters
            if (!sm.HasBeenPowered)
                continue;

            supermatterUids.Add(supermatterUid);
        }

        if (supermatterUids.Count == 0)
            return;

        base.Added(uid, component, gameRule, args);

        component.SupermatterUid = _random.Pick(supermatterUids);

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
            colorOverride: Color.Gold
        );
    }

    /// <summary>
    /// Adjusts the supermatters power and heat modifier to a specified random value alongside timing the explosive lightning.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        if (sm.Event != SupermatterEvent.Surging)
        {
            sm.Event = SupermatterEvent.Surging;
            component.NextLightningTime = Timing.CurTime + TimeSpan.FromSeconds(component.LightningCooldownMinMax.Next(_random));
        }

        // Power & heat modifer changes every tick so isn't always used by the supermatter, but creates a good visual on the console
        sm.Power = component.PowerMinMax.Next(_random);
        sm.HeatModifier = _random.NextFloat(component.HeatModifierMinMax.Item1, component.HeatModifierMinMax.Item2);

        if (Timing.CurTime < component.NextLightningTime)
            return;
        else
        {
            // Explosive supermatter lightning strikes
            _lightning.ShootRandomLightnings(component.SupermatterUid, component.ZapRange, component.ZapCount, sm.LightningPrototypes[2]);

            component.NextLightningTime += TimeSpan.FromSeconds(component.LightningCooldownMinMax.Next(_random));
        }
    }

    /// <summary>
    /// Removes the supermatter surge event from the supermatter.
    /// </summary>
    protected override void Ended(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Event = SupermatterEvent.None;
    }
}
