using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Effigy locator config and runtime state.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherEffigyLocatorComponent : Component
{
    /// <summary>
    /// The action prototype granted while the Slasher has an effigy.
    /// </summary>
    [DataField]
    public EntProtoId ActionPrototype { get; set; } = "ActionSlasherLocateEffigy";

    /// <summary>
    /// The pinpointer prototype spawned into the temporary hand.
    /// </summary>
    [DataField]
    public EntProtoId PinpointerPrototype { get; set; } = "PinpointerSlasherEffigy";

    /// <summary>
    /// The temporary locator hand name.
    /// </summary>
    [DataField]
    public string HandId { get; set; } = "slasherEffigyLocatorHand";

    /// <summary>
    /// The granted locate-effigy action entity, if present.
    /// </summary>
    public EntityUid? ActionEntity { get; set; }

    /// <summary>
    /// The spawned locator pinpointer, if active.
    /// </summary>
    public EntityUid? PinpointerEntity { get; set; }
}