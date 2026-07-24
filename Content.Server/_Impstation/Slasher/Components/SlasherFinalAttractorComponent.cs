using Content.Server._Impstation.Slasher;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Stores timing and search settings for the Slasher final boss target-selection pass.
/// </summary>
[RegisterComponent]
[Access(typeof(SlasherFinalChaseTargetSystem), typeof(SlasherFinalEntitySystem))]
public sealed partial class SlasherFinalChaseTargetComponent : Component
{
    /// <summary>
    /// Unused chase-strength legacy field kept for YAML compatibility.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseRange { get; set; } = 20f;

    /// <summary>
    /// Spatial query radius for finding nearby crew.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSearchRange { get; set; } = 25f;

    /// <summary>
    /// How often to refresh the chase target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TargetPulsePeriod { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Last time this entity refreshed its chase target. Managed by SlasherFinalChaseTargetSystem.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [Access(typeof(SlasherFinalChaseTargetSystem), typeof(SlasherFinalEntitySystem))]
    public TimeSpan LastPulseTime { get; set; } = default!;
}
