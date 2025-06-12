using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Egg;

public sealed partial class XenoEggPlaceEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId Prototype = "XenoEgg";
}
