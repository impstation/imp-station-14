namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Placed on the gain-shroud action entity.
/// Controls the configurable values for entering stealth.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherShroudActionComponent : Component
{
    /// <summary>
    /// How many bleed stacks to apply to the Slasher when they activate shroud.
    /// </summary>
    [DataField]
    public float BleedAmount { get; set; } = 2f;

    /// <summary>
    /// How often bleed is reapplied while shrouded.
    /// </summary>
    [DataField]
    public float BleedReapplyDelay { get; set; } = 3f;

    /// <summary>
    /// Maximum fraction of the user's bleed cap that shroud is allowed to fill.
    /// </summary>
    [DataField]
    public float BleedCapRatio { get; set; } = 0.5f;

    /// <summary>
    /// How fast visibility increases per unit of distance moved while shrouded.
    /// </summary>
    [DataField]
    public float MovementVisibilityRate { get; set; } = 0f;

    /// <summary>
    /// Minimum visibility when the Slasher is stationary.
    /// </summary>
    [DataField]
    public float MinVisibility { get; set; } = -1f;
}

/// <summary>
/// Marker component for the lose-shroud action entity.
/// Exists so the leave action can stay explicit without carrying unused configuration.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherUnshroudActionComponent : Component
{
}
