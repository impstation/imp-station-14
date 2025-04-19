using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.WashingMachine.Components;

/// <summary>
/// Attached to a washing machine that is currently in the process of washing
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveWashingMachineComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float WashTimeRemaining;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;
}
