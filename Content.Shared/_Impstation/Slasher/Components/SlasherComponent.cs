using Robust.Shared.Prototypes;

namespace Content.Shared.Slasher;

[RegisterComponent]
public sealed partial class SlasherComponent : Component
{
    [DataField]
    public EntProtoId DestroyActionPrototype = "ActionDestroy";

    [DataField]
    public EntityUid? DestroyAction;
}
