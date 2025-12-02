using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic;

[Serializable, NetSerializable]
public sealed partial class FleshGraspDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized] public EntityUid Target;

    public FleshGraspDoAfterEvent(EntityUid target)
    {
        Target = target;
    }
}
