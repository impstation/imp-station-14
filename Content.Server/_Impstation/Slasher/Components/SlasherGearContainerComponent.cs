using Content.Shared.Roles;
using Content.Shared.Thief;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marks a ThiefUndeterminedBackpack entity as a Slasher cosmetic gear container.
/// Maps each possible set selection to the corresponding StartingGear variant prototype,
/// so the reclaim system can determine which base-kit slots the chosen kit replaces.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGearContainerSystem))]
public sealed partial class SlasherGearContainerComponent : Component
{
    /// <summary>
    /// List of sets available for selection.
    /// </summary>
    [DataField]
    public List<ProtoId<ThiefBackpackSetPrototype>> PossibleSets { get; set; } = new();

    /// <summary>
    /// Indices of the sets the player has currently selected in the UI.
    /// </summary>
    [DataField]
    public List<int> SelectedSets { get; set; } = new();

    /// <summary>
    /// Sound played when the player confirms their chosen cosmetic set.
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier ApproveSound { get; set; } = new("storageRustle");

    /// <summary>
    /// Max number of sets you can select.
    /// </summary>
    [DataField]
    public int MaxSelectedSets { get; set; } = 1;

    /// <summary>
    /// Title field for the gear UI.
    /// </summary>
    [DataField]
    public LocId ToolName { get; set; } = "thief-backpack-window-title";

    /// <summary>
    /// Description field for the gear UI.
    /// </summary>
    [DataField]
    public LocId ToolDesc { get; set; } = "thief-backpack-window-description";

    /// <summary>
    /// What entity all the spawned items will appear inside of.
    /// If null, items will be dropped on the ground instead.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnedStoragePrototype { get; set; }

    /// <summary>
    /// Maps thiefBackpackSet prototype IDs to their corresponding StartingGear variant prototype IDs.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ThiefBackpackSetPrototype>, ProtoId<StartingGearPrototype>> SetToVariantGear { get; set; } = new();
}
