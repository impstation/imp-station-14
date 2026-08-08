using Content.Client.Eui;
using Content.Shared._Impstation.NanoChat;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Impstation.NanoChat;

[UsedImplicitly]
public sealed class AdminNanoChatLogsEui : BaseEui
{
    private readonly AdminNanoChatLogsWindow _window;

    public AdminNanoChatLogsEui()
    {
        _window = new AdminNanoChatLogsWindow();
        _window.OnClose += () => SendMessage(
            new AdminNanoChatLogsEuiMsg.Close());
        _window.OnRefresh += () => SendMessage(
            new AdminNanoChatLogsEuiMsg.RefreshLogs());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminNanoChatLogsEuiState cast)
            return;
        _window.PopulateLogs(cast.Logs);
    }
}
