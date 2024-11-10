using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Gravity;
public abstract partial class SharedZeroGravityAreaComponent : Component
{
    [DataField(readOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public string Fixture = "antiGravity";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}

[Serializable, NetSerializable]
public sealed partial class ZeroGravityAreaState : ComponentState
{
    public ZeroGravityAreaState(SharedZeroGravityAreaComponent comp)
    {
        Enabled = comp.Enabled;
    }

    public bool Enabled;
}
