using Content.Shared.Radiation.Components;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared.Radiation.Systems;

public abstract class SharedRadiationPulseSystem : EntitySystem // imp, changed from sealed to abstract and added Shared
{
    [Dependency] protected readonly IGameTiming Timing = default!; // imp, changed from private to protected and from _timing to Timing

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationPulseComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, RadiationPulseComponent component, ComponentStartup args)
    {
        component.StartTime = Timing.RealTime; // imp, changed from _timing to Timing

        // try to get despawn time or keep default duration time
        if (TryComp<TimedDespawnComponent>(uid, out var despawn))
        {
            component.VisualDuration = despawn.Lifetime;
        }
        // try to get radiation range or keep default visual range
        if (TryComp<RadiationSourceComponent>(uid, out var radSource))
        {
            component.VisualRange = radSource.Intensity / radSource.Slope;
        }
    }
}
