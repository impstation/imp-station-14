namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Configuration for actions that heal only when the user is in sufficiently low luminance.
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
    /// Maximum luminance value that still permits this heal action.
    /// Server evaluation uses less-than-or-equal comparison (luminance <= threshold).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AmbientDarkThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Bleed delta applied on successful dark heal to close active bleeding.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BleedClosureAmount { get; set; } = -1000f;
}
