using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic;

[Serializable, NetSerializable]
public sealed partial class FleshGraspDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized] public new EntityUid Target; // imp added "new" keyword

    public FleshGraspDoAfterEvent(EntityUid target)
    {
        Target = target;
    }
}
