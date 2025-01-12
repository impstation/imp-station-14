using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AttachItemToHandComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Item;

    [DataField , AutoNetworkedField]
    public EntityUid ItemEntity;

    [DataField]
    public string SlotId = "";
}

