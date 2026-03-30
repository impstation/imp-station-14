using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Server.GameTicking;
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
    [Dependency] private readonly GameTicker _ticker = default!;

    protected override void Added(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var supermatterUids = new List<EntityUid>();
        var query = EntityQueryEnumerator<SupermatterComponent>();

        while (query.MoveNext(out var supermatterUid, out _))
        {
            supermatterUids.Add(supermatterUid);
        }

        if (supermatterUids.Count == 0)
        {
            // Means a scheduled event can be skipped on maps without supermatter, but hopefully the rarity of the event offsets that
            _ticker.EndGameRule(uid, gameRule);
            return;
        }

        component.SupermatterUid = _random.Pick(supermatterUids);
        // When the supermatter surge starts can be randomized if desired similar to how NextLightningTime is done
        component.SurgeStartTime = Timing.CurTime + component.SurgeStartLength;
        // Dosen't start explodings stuff immediately
        component.NextLightningTime = component.SurgeStartTime + TimeSpan.FromSeconds(component.LightningCooldownMinMax.Next(_random));

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
            colorOverride: Color.Gold
        );
    }

    protected override void ActiveTick(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime < component.SurgeStartTime)
            return;

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Surging = true;

        var powerSurge = component.PowerMinMax.Next(_random);
        var heatSurge = (float)component.HeatModifierMinMax.Next(_random) / 100;

        // Power & heat modifer changes every tick so isn't always used by the supermatter, but creates a good visual on the console
        sm.Power = powerSurge;
        sm.HeatModifier = heatSurge;

        if (Timing.CurTime < component.NextLightningTime)
            return;
        else
        {
            // Explosive supermatter lightning strikes
            _lightning.ShootRandomLightnings(component.SupermatterUid, component.ZapRange, component.ZapCount, sm.LightningPrototypes[2]);

            component.NextLightningTime += TimeSpan.FromSeconds(component.LightningCooldownMinMax.Next(_random));
        }
    }

    protected override void Ended(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Surging = false;
    }
}
