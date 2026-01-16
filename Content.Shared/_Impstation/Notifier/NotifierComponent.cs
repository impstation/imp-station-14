using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public bool Active = false;
}
