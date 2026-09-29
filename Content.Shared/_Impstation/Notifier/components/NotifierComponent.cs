using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotifierComponent : Component
{
    /// <summary>
    /// The player attached to the entity
    /// </summary>
    [AutoNetworkedField]
    public NetUserId AttachedUserId { get; set; } = new NetUserId();

    /// <summary>
    /// The contents of the notifier
    /// </summary>
    [AutoNetworkedField]
    public PlayerNotifierSettings Settings { get; set; } = new PlayerNotifierSettings();

    /// <summary>
    /// List of entities copying this notifier
    /// </summary>
    public List<NetEntity> Copycats = new List<NetEntity>();
}
