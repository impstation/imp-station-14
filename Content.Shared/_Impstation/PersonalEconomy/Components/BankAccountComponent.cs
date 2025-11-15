using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This is used for keeping track of the data of one bank account
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankAccountComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name = string.Empty;

    [DataField, AutoNetworkedField]
    public int AccessNumber = 0;
    [DataField, AutoNetworkedField]
    public int TransferNumber = 0;

    [DataField, AutoNetworkedField]
    public int Balance = 0;
    [DataField, AutoNetworkedField]
    public int Salary = 0;

    //todo make this a server-only property in the comp, then have the data get sent back to the client via a BUI state?
    //or just have it send a normal message back to the client system that then caches the values?
    //or just do a field delta state for this field specifically?
    //or ent-ify bank accounts? it'll be like 50 ents with 3 comps that get force-PVS'd to everyone which feels kinda sussy but it does massively reduce the size of each state
    //basically I don't want to be replicating every transaction to every player all the time because that will potentially be a lot of network traffic
    //ok yeah I think I've convinced myself that ent-ifying bank accounts is the way to go?
    //I'll only store 10 transactions, also this lets everything be in shared, so everything seems good?
    //let this be a testament to my madness (and 1 am programming prowess)
    [DataField, AutoNetworkedField]
    public List<BankTransaction> Transactions = []; //todo make this only be the last like 10-15 transactions?
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BankTransaction
{
    [DataField] public int Other = 0;
    [DataField] public int Amount = 0;
    [DataField] public string Reason = string.Empty; //limited to 64 chars
}
