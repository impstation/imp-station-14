using Robust.Shared.GameStates;

namespace Content.Client._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
public sealed partial class IsInZeroGravityAreaComponent : Component
{
    public bool IsWeightless => AreaFingerprint != 0;

    /// <inheritdoc cref="Content.Shared._Impstation.Gravity.IsInZeroGravityAreaState.AreaFingerprint"/>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ulong AreaFingerprint = 0;
}
