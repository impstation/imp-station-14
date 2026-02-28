using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This stores the destination account, charge & reason for a PoS system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PosSystemComponent : Component
{
    [AutoNetworkedField]
    public TransferNumber RecipientAccount = 0;

    [AutoNetworkedField]
    public int Amount = 0;

    [AutoNetworkedField]
    public string Reason = "";

}
