namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Tracks the active cosmetic gear variant selected by this Slasher.
/// Set when the Slasher approves a gear container selection.
/// Read at death-maze reclaim time to determine which base-kit slots are suppressed.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherCosmeticVariantComponent : Component
{
    /// <summary>
    /// The StartingGear prototype ID of the selected cosmetic variant (e.g. SlasherGearClown).
    /// Null if no cosmetic kit has been approved.
    /// </summary>
    [DataField]
    public string? SelectedVariantGearId { get; set; }
}
