using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This is used to keep track of what bank accounts exist and how much money they have
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteBankServerComponent : Component
{
    //does this even need to exist if accounts are ents?
    //theoretically this is good for a cache but we can just build that in the shared system by subbing to comp startup / shutdown in the shared system?
    /// <summary>
    /// the one that stays on the server, the more detailed list of every acct's actions
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, EntityUid> AccountDict = [];

    /// <summary>
    /// Mapping of transfer numbers to actual account numbers
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, int> TransferNumberToAccountNumberDict = [];
}
