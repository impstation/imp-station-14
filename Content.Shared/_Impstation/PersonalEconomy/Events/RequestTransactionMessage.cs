using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

/// <summary>
/// Request that a transaction attempt is made.
/// </summary>
/// <param name="senderAccount"> the sender's account access number</param>
/// <param name="recipientAccount"> the recipient's account transfer number</param>
/// <param name="amount"> the amount of money to be transferred</param>
/// <param name="reason"> the reason for the transfer</param>
[Serializable, NetSerializable]
//todo will probably need different types of message / transactions
//mainly for "sending" money to arbitrary people
public sealed class RequestTransactionMessage(AccessNumber senderAccount, TransferNumber recipientAccount, int amount, string reason)
    : BoundUserInterfaceMessage
{
    public AccessNumber SenderAccount = senderAccount;
    public TransferNumber RecipientAccount = recipientAccount;
    public int Amount = amount;
    public string Reason = reason;
}
