using Robust.Shared.GameStates;

namespace Content.Client.Gravity;

[RegisterComponent, NetworkedComponent]
public sealed partial class IsInZeroGravityAreaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsWeightless = false;
}
