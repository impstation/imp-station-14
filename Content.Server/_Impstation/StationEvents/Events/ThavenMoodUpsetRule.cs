using System.Linq;
using Content.Server._Impstation.StationEvents.Components;
using Content.Server._Impstation.StrangeMoods;
using Content.Server.StationEvents.Events;
using Content.Shared._Impstation.StrangeMoods;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Impstation.StationEvents.Events;
public sealed class ThavenMoodUpsetRule : StationEventSystem<ThavenMoodUpsetRuleComponent>
{
    [Dependency] private readonly StrangeMoodsSystem _moodsSystem = default!;

    protected override void Started(EntityUid uid, ThavenMoodUpsetRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // if we want to a new shared mood across thavens, store it so we don't generate unique shared moods
        if (component.NewSharedMoods)
        {
            // on imp the only shared mood is "thaven"... but this could be expanded on
            // for anything with StrangeMoods to get hit with a shared mood rotate.
            var sharedMoodIds = _moodsSystem.GetSharedMoods().Select(m => m.UniqueId).ToList();
            foreach (var moodId in sharedMoodIds)
            {
                // this shouldnt be empty but weirder has happened
                if (String.IsNullOrEmpty(moodId))
                    continue;
                _moodsSystem.NewSharedMoods(moodId, clearOldMoods: true);
            }
        }

        var thavens = EntityQueryEnumerator<StrangeMoodsComponent>();
        while (thavens.MoveNext(out var thavenUid, out var thavenComp))
        {
            // refresh the moods of the thavens
            _moodsSystem.RefreshMoods((thavenUid, thavenComp));

            // if they have their shared moods updated, no need to renotify them of a mood change
            // as upon shared mood change, it calls NotifyMoodChange already.
            if(!component.NewSharedMoods)
                _moodsSystem.NotifyMoodChange((thavenUid, thavenComp));
        }
    }
}
