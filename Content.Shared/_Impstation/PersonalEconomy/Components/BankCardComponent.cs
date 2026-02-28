using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// The component that indicates an entity is a bank card. only keeps track of the account it is connected to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankCardComponent : Component
{

    [DataField, AutoNetworkedField]
    public AccessNumber AccessNumber;

    [DataField, AutoNetworkedField]
    public TransferNumber TransferNumber;

    [DataField, AutoNetworkedField]
    public string Name = "Unknown";

    [DataField]
    public LocId NamedLocId = "bank-card-name";

    /// <summary>
    /// Here as it's the easiest way to set a starting value per-job, like with IDs
    /// </summary>
    [DataField]
    public int StartingBalance = 10000;

    /// <summary>
    /// Here as it's the easiest way to set a starting value per-job, like with IDs
    /// </summary>
    [DataField]
    public int Salary = 500;
}
