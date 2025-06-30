using Content.Shared._Impstation.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.NotifierExamine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierExamineComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public bool Active = false;

    [DataField(required: true), AutoNetworkedField]
    public bool IconOn = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public ProtoId<NotifierIconPrototype> Icon = "NotifierIcon";

}
