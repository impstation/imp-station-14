using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Content.Server.Announcements.Systems; // ee announce
using Robust.Shared.Player; // ee announce

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class FalseAlarmRule : StationEventSystem<FalseAlarmRuleComponent>
{
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!; // ee announce

    protected override void Started(EntityUid uid, FalseAlarmRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        base.Started(uid, component, gameRule, args); // imp

        var allEv = _event.AllEvents()
            .Where(p => p.Value.StartAnnouncement) // imp where
            .Select(p => p.Key).ToList(); // imp key
        var picked = RobustRandom.Pick(allEv);

        _announcer.SendAnnouncement( // ee announce
            _announcer.GetAnnouncementId(picked.ID),
            Filter.Broadcast(),
            _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(picked.ID)),
            colorOverride: Color.Gold,
            //TODO This isn't a good solution, but I can't think of something better
            localeArgs: ("data", Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}"))
        );
    }
}
