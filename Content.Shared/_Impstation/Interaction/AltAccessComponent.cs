using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AltAccessComponent : Component
{

    /// <summary>
    /// The action id used to open the relevant UI
    /// </summary>
    [DataField]
    public EntProtoId? AltAccessAction;

    /// <summary>
    /// Valid inventory slots the action can be used in. None by default
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.NONE;

    [DataField, AutoNetworkedField]
    public EntityUid? AltAccessEntity;



}
