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

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
            colorOverride: Color.Gold
        );
    }

    protected override void ActiveTick(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Surge = true;

        var powerSurge = _random.NextFloat(component.MinPowerSurge, component.MaxPowerSurge);
        var heatSurge = _random.NextFloat(component.MinHeatSurge, component.MaxHeatSurge);

        // Power & heat modifer changes every tick so isn't always used by the supermatter, but creates a good visual on the console
        sm.Power = powerSurge;
        sm.HeatModifier = heatSurge;

        if (component.TimeUntilNextLightning > 0)
            component.TimeUntilNextLightning -= frameTime;
        else
        {
            // Explosive supermatter lightning strikes
            _lightning.ShootRandomLightnings(component.SupermatterUid, component.ZapRange, component.ZapCount, sm.LightningPrototypes[2], arcDepth: component.ZapArcDepth);

            component.TimeUntilNextLightning += _random.NextFloat(component.MinTimeForLightning, component.MaxTimeForLightning);
        }
    }

    protected override void Ended(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Surge = false;
    }
}
