using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true), AutoNetworkedField]
    public FormattedMessage Content = FormattedMessage.Empty;

    [DataField(required: true), AutoNetworkedField]
    public bool Active = false;
}
