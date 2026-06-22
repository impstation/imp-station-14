using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierCopycatComponent :  Component
{
    /// <summary>
    /// The original user being copied
    /// </summary>
    [AutoNetworkedField]
    public NetUserId OriginUserId { get; set; } = new NetUserId();

    /// <summary>
    /// Contents of the notifier.
    /// </summary>
    [AutoNetworkedField]
    public PlayerNotifierSettings Settings { get; set; } = new PlayerNotifierSettings();
}


