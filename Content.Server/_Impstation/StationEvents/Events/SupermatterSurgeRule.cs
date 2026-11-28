using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Lightning;
using Content.Server.StationEvents.Events;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class SupermatterSurgeRule : StationEventSystem<SupermatterSurgeRuleComponent>
{
    [Dependency] private readonly LightningSystem _lightning = default!;

    /// <summary>
    /// Finding an active supermatter for the event.
    /// </summary>
    protected override void Added(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var supermatterUids = new List<EntityUid>();
        var query = EntityQueryEnumerator<SupermatterComponent>();
        while (query.MoveNext(out var supermatterUid, out var sm))
        {
            // Does not target shards or inactive supermatters
            if (sm.IsShard || !sm.HasBeenPowered)
                continue;

            supermatterUids.Add(supermatterUid);
        }

        if (supermatterUids.Count == 0)
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        base.Added(uid, component, gameRule, args);

        component.SupermatterUid = RobustRandom.Pick(supermatterUids);
    }

    protected override void Started(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Event = SupermatterEvent.Surging;
        component.NextLightningTime = Timing.CurTime + TimeSpan.FromSeconds(component.LightningCooldown.Next(RobustRandom));
    }

    /// <summary>
    /// Adjusts the supermatters power and heat modifier to a specified random value alongside timing the explosive lightning.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, SupermatterSurgeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        // Power & heat modifer changes every tick so isn't always used by the supermatter, but creates a good visual on the console
        sm.Power = component.Power.Next(RobustRandom);
        sm.HeatModifier = RobustRandom.NextFloat(component.HeatModifier.Min, component.HeatModifier.Max);

        if (Timing.CurTime < component.NextLightningTime)
            return;

        // Explosive supermatter lightning strikes
        _lightning.ShootRandomLightnings(component.SupermatterUid, component.ZapRange, component.ZapCount, sm.LightningPrototypes[2], arcDepth: 1);
        component.NextLightningTime = Timing.CurTime + TimeSpan.FromSeconds(component.LightningCooldown.Next(RobustRandom));
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
