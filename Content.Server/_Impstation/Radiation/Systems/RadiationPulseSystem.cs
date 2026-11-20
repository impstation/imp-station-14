using Content.Shared._Impstation.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.Radiation.Systems;

public sealed class RadiationPulseSystem : SharedRadiationPulseSystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    private const float VisualRangeFilterAddition = 20f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationPulseComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Timing.CurTime < comp.NextInRangeEvent)
                continue;

            comp.NextInRangeEvent = Timing.CurTime + comp.InRangeEventInterval;

            var coords = _xform.GetMapCoordinates(uid);
            var ev = new RadiationPulseInRangeEvent(GetNetEntity(uid), coords, comp.NextInRangeEvent, comp.StartTime, comp.VisualDuration, comp.VisualRange);
            RaiseNetworkEvent(ev, Filter.Empty().AddInRange(coords, comp.VisualRange + VisualRangeFilterAddition, entMan: EntityManager));
        }
    }
}
