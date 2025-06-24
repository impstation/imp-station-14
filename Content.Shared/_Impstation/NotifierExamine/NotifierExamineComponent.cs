using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.NotifierExamine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierExamineComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public bool Active = false;

}
