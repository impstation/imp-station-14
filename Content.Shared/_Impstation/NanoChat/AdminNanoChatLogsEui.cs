using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.NanoChat;

/// <summary>
///     A new EUI state for the Admin NanoChat Logs ui.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminNanoChatLogsEuiState : EuiStateBase
{
    public List<AdminNanoChatLogEntry> Logs { get; }

    public AdminNanoChatLogsEuiState(List<AdminNanoChatLogEntry> logs)
    {
        Logs = logs;
    }
}

/// <summary>
///     A new nanochatlogs entry that will be converted into a rich text label.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminNanoChatLogEntry
{
    public NetUserId SenderUser { get; }
    public string Sender { get; }
    public string Message { get; }
    public TimeSpan Timestamp { get; }
    public string Card { get; }
    public string Recipients { get; }

    public AdminNanoChatLogEntry(
        NetUserId senderUser,
        string sender,
        string message,
        TimeSpan timestamp,
        string card,
        string recipients)
    {
        SenderUser = senderUser;
        Sender = sender;
        Message = message;
        Timestamp = timestamp;
        Card = card;
        Recipients = recipients;
    }
}

public static class AdminNanoChatLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase { }

    [Serializable, NetSerializable]
    public sealed class RefreshLogs : EuiMessageBase { }
}
