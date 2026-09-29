using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Actions;

/// <summary>
/// Makes popups when summoning and retracting when on an action with RetractableItemActionComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PopupOnRetractableItemActionComponent : Component
{
    /// <summary>
    /// The text this popup will display to the recipient when unretracted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId UnretractedText;

    /// <summary>
    /// The text this popup will display to everything but the recipient when unretracted.
    /// If left null this will reuse <see cref="UnretractedText"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? UnretractedOtherText;

    /// <summary>
    /// The text this popup will display to the recipient when retracted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId RetractedText;

    /// <summary>
    /// The text this popup will display to everything but the recipient when retracted.
    /// If left null this will reuse <see cref="RetractedText"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? RetractedOtherText;
}
