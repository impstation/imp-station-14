using Content.Server._Impstation.StationEvents.Components;
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

public sealed class SupermatterEventRule : StationEventSystem<SupermatterEventRuleComponent>
{
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    protected override void Added(EntityUid uid, SupermatterEventRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var supermatterUids = new List<EntityUid>();
        var query = EntityQueryEnumerator<SupermatterComponent>();

        while (query.MoveNext(out var supermatterUid, out _))
            supermatterUids.Add(supermatterUid);

        // If a supermatter exists, then start a gamerule based around one
        if (!(supermatterUids.Count == 0))
        {
            _ticker.StartGameRule(_random.Pick(component.SupermatterEvents));
            _ticker.EndGameRule(uid, gameRule);
            return;
        }

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
            colorOverride: Color.Gold
        );
    }

    protected override void Ended(EntityUid uid, SupermatterEventRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        if (!TryFindRandomTile(out var _, out var _, out var _, out var coords))
            return;

        _emp.EmpPulse(coords, component.EMPRange, component.EmpEnergyConsumption, component.EmpDisabledDuration);

        base.Ended(uid, component, gameRule, args);
    }
}
