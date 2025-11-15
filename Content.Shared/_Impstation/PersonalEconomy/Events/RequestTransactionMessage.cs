using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

[Serializable, NetSerializable]
public sealed class RequestTransactionMessage(int fromAccount, int toAccount, int amount, string reason)
    : BoundUserInterfaceMessage
{
    public int FromAccount = fromAccount;
    public int ToAccount = toAccount;
    public int Amount = amount;
    public string Reason = reason;
}
