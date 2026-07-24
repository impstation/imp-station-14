namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Tracks active Slasher shroud upkeep values while hidden.
/// This exists so the server can remember how much bleed the shroud is responsible for,
/// top that bleed back up while hidden, remove that same amount when the shroud ends,
/// and keep track of which of the paired shroud toggle actions is currently live.
/// </summary>
[RegisterComponent, Access(typeof(SlasherShroudActionSystem))]
public sealed partial class SlasherShroudComponent : Component
{
    /// <summary>
    /// Currently active gain-shroud action entity while the user is visible.
    /// Exactly one of the paired shroud action references should be populated at a time.
    /// This is null while the user is already shrouded.
    /// </summary>
    public EntityUid? ActiveGainShroudActionEntity { get; set; }

    /// <summary>
    /// Currently active lose-shroud action entity while the user is hidden.
    /// This is null while the user is visible.
    /// </summary>
    public EntityUid? ActiveLoseShroudActionEntity { get; set; }

    /// <summary>
    /// Bleed added each upkeep tick while shrouded.
    /// </summary>
    public float BleedAmount { get; set; }

    /// <summary>
    /// Maximum fraction of max bleed the shroud is allowed to maintain.
    /// </summary>
    public float BleedCapRatio { get; set; }

    /// <summary>
    /// Total bleed currently attributed to the shroud and removed when it ends.
    /// </summary>
    public float ShroudBleedApplied { get; set; }

    /// <summary>
    /// Time between bleed upkeep ticks while hidden.
    /// </summary>
    public TimeSpan BleedReapplyDelay { get; set; }

    /// <summary>
    /// Next world time when the shroud should reapply bleed.
    /// </summary>
    public TimeSpan NextBleedTime { get; set; }
}
