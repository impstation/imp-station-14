using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

[Serializable, NetSerializable]
public sealed class UpdatePoSSettingsMessage(TransferNumber recipient, int amount, string reason) : BoundUserInterfaceMessage
{
    public TransferNumber Recipient = recipient;
    public int Amount = amount;
    public string Reason = reason;
}
