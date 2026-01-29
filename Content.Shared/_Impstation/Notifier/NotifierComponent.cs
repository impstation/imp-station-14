using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierComponent : Component
{
    [AutoNetworkedField]
    public NetUserId AttachedUserId { get; set; } = new NetUserId();
}
