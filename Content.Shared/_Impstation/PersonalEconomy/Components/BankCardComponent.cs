using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// The component that indicates an entity is a bank card. only keeps track of the account it is connected to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankCardComponent : Component
{

    [DataField, AutoNetworkedField]
    public int AccessNumber = 0; //todo this can probably just be a number that gets formatted actually?

    [DataField, AutoNetworkedField]
    public int TransferNumber = 0;

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
