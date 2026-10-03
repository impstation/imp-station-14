//using Content.Server.Silicons.Laws; // macro remove
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random; // macro

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    // [Dependency] private IonStormSystem _ionStorm = default!; // macro remove
    [Dependency] private readonly IRobustRandom _random = default!; // macro

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        // begin macro edit
        // var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var target, out var xform))
        // end macro edit
        {
            // only affect law holders on the station, and check random chance (macro edit)
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation ||
                !_random.Prob(target.Chance)) // macro
                continue;
            // begin macro edit again
            var ev = new IonStormEvent();
            RaiseLocalEvent(ent, ref ev);
            //     _ionStorm.IonStormTarget((ent, lawBound, target)); // end macro
        }
    }
}

// macro add
/// <summary>
/// Event raised on an entity with <see cref="IonStormTargetComponent"/> when an ion storm occurs on the attached station.
/// </summary>
[ByRefEvent]
public record struct IonStormEvent(bool Adminlog = true);
