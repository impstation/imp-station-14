using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.WashingMachine.Components;

/// <summary>
/// Attached to a washing machine that is currently in the process of washing
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveWashingMachineComponent : Component
{
    /// <summary>
    /// Remaining time before cycle ends
    /// </summary>
    [DataField]
    public float WashTimeRemaining;

    /// <summary>
    /// Actual time of cycle, unaffected by multiplier
    /// </summary>
    [DataField]
    public float TotalTime;

    /// <summary>
    /// How long between rolls for malfunction
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;
}
