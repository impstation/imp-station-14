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
    public NetUserId Sender { get; }
    public string Message { get; }

    public AdminNanoChatLogEntry(
        NetUserId senderUser,
        string message)
    {
        Sender = senderUser;
        Message = message;
    }
}

public static class AdminNanoChatLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase { }

    [Serializable, NetSerializable]
    public sealed class RefreshLogs : EuiMessageBase { }
}
