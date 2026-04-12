namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marks an item as belonging to a specific Slasher by mind entity.
/// This allows add/remove/reassign ownership when loadouts change.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherItemOwnershipComponent : Component
{
    /// <summary>
    /// Mind entity that currently owns this Slasher item.
    /// Mind identity is stable if the body changes.
    /// </summary>
    [DataField]
    public EntityUid? OwnerMind;

    /// <summary>
    /// Source category for future reclaim policy tuning.
    /// </summary>
    [DataField]
    public SlasherOwnedItemSource Source = SlasherOwnedItemSource.StarterKit;

    /// <summary>
    /// Optional slot metadata reserved for future appearance-loadout support.
    /// </summary>
    [DataField]
    public string? Slot;
}

/// <summary>
/// Enumeration values for SlasherOwnedItemSource.
/// </summary>
public enum SlasherOwnedItemSource
{
    StarterKit,
    AppearanceLoadout,
}
