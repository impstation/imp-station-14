using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This is used to keep track of what bank accounts exist and how much money they have
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteBankServerComponent : Component
{
    //for now I'm being lazy and just sending all the data to every client, someone yell at me later if it's a problem, I know where & how I can do the client-server split
    /// <summary>
    /// the one that stays on the server, the more detailed list of every acct's actions
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, BankAccount> AccountDict = [];

    /// <summary>
    /// Mapping of transfer numbers to actual account numbers
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, int> TransferNumberToAccountNumberDict = [];
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BankAccount
{
    [DataField] public string Name = string.Empty;

    [DataField] public int AccessNumber = 0;
    [DataField] public int TransferNumber = 0;

    [DataField] public int Balance = 0;
    [DataField] public int Salary = 0;

    [DataField] public List<BankTransaction> Transactions = []; //todo make this only be the last like 10-15 transactions?
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BankTransaction
{
    [DataField] public bool Outgoing = false;
    [DataField] public string Other = string.Empty;
    [DataField] public int Amount = 0;
    [DataField] public string Reason = string.Empty; //todo limit this to like 64 chars or so
}
