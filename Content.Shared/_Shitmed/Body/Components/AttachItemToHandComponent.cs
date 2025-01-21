using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Body.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class AttachItemToHandComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Item;

    [DataField , AutoNetworkedField]
    public EntityUid ItemEntity;

    [DataField]
    public string SlotId = "";
}

