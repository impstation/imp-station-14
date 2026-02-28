using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

[Serializable, NetSerializable]
public sealed class PoSTransactionFailedMessage(AccessNumber account) : BoundUserInterfaceMessage
{
    public AccessNumber Account = account;
}
