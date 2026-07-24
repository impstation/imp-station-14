namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Dark-heal config.
/// </summary>
[RegisterComponent]
public sealed partial class LightLevelHealActionComponent : Component
{
    /// <summary>
    /// Total healing applied when the action succeeds.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HealAmount { get; set; } = 50f;

    /// <summary>
    /// Maximum luminance that still allows healing.
    /// The server checks luminance <= threshold.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AmbientDarkThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Bleed delta applied on a successful dark heal.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BleedClosureAmount { get; set; } = -1000f;
}
