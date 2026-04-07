using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Announcements.Systems;
using Content.Server.Emp;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Events;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class SupermatterDischargeRule : StationEventSystem<SupermatterDischargeRuleComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Added(EntityUid uid, SupermatterDischargeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var supermatterUids = new List<EntityUid>();
        var query = EntityQueryEnumerator<SupermatterComponent>();

        while (query.MoveNext(out var supermatterUid, out _))
            supermatterUids.Add(supermatterUid);

        if (supermatterUids.Count == 0)
        {
            // Means a scheduled event can be skipped on maps without supermatter, but hopefully the rarity of the event offsets that
            _ticker.EndGameRule(uid, gameRule);
            return;
        }

        component.SupermatterUid = _random.Pick(supermatterUids);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        // Dosen't start discharging a emp immediately
        component.NextEMPTime = Timing.CurTime + TimeSpan.FromSeconds(component.EMPCooldownMinMax.Next(_random));
        component.BaseGasEfficiency = sm.GasEfficiency;
        sm.GasEfficiency = component.DischargeGasEfficiency; // Also controls how much mix is merged into the atmosphere

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
            colorOverride: Color.Gold
        );
    }

    protected override void ActiveTick(EntityUid uid, SupermatterDischargeRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Event = SupermatterEvent.Discharging;
        var mix = _atmosphere.GetTileMixture(component.SupermatterUid, true);

        if (mix is { } && sm.GasStorage is { })
            _atmosphere.Merge(mix, sm.GasStorage);

        if (Timing.CurTime < component.NextEMPTime)
            return;
        else
        {
            // Supermatter EMP discharge
            _emp.EmpPulse(_transform.GetMapCoordinates(uid), component.EMPRange, component.EmpEnergyConsumption, component.EmpDisabledDuration);

            component.NextEMPTime += TimeSpan.FromSeconds(component.EMPCooldownMinMax.Next(_random));
        }
    }

    protected override void Ended(EntityUid uid, SupermatterDischargeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<SupermatterComponent>(component.SupermatterUid, out var sm))
            return;

        sm.Event = SupermatterEvent.None;
        sm.GasEfficiency = component.BaseGasEfficiency;
    }
}
