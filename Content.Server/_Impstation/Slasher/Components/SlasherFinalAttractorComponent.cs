using Content.Server._Impstation.Slasher;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Makes the SlasherFinalEntity drift toward living crew via RandomWalk bias,
/// mirroring how SingularityAttractorComponent biases the singulo toward powered attractors.
/// </summary>
[RegisterComponent]
[Access(typeof(SlasherFinalAttractorSystem))]
public sealed partial class SlasherFinalAttractorComponent : Component
{
    /// <summary>
    /// Scales how strongly nearby crew attract the boss.
    /// Equivalent to SingularityAttractorComponent.BaseRange.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseRange { get; set; } = 20f;

    /// <summary>
    /// Spatial query radius for finding nearby crew.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSearchRange { get; set; } = 25f;

    /// <summary>
    /// How often to pulse and rebias the RandomWalk.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TargetPulsePeriod { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Last time this entity pulsed. Managed by SlasherFinalAttractorSystem.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [Access(typeof(SlasherFinalAttractorSystem))]
    public TimeSpan LastPulseTime { get; set; } = default!;
}
