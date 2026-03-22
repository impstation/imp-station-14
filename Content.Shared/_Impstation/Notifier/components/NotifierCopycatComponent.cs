using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierCopycatComponent :  Component
{
    [AutoNetworkedField]
    public NetUserId OriginUserId { get; set; } = new NetUserId();

    [AutoNetworkedField]
    public PlayerNotifierSettings Settings { get; set; } = new PlayerNotifierSettings();
}


