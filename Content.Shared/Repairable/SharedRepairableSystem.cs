using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract partial class SharedRepairableSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public sealed partial class RepairFinishedEvent : SimpleDoAfterEvent // imp, protected to public.
    {
    }
}

