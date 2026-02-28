using Content.Server.EUI;
using Content.Shared._Impstation.NanoChat;
using Content.Shared._Impstation.Station.Components;
using Content.Shared.Eui;

namespace Content.Server._Impstation.NanoChat;

public sealed class AdminNanoChatLogsEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public AdminNanoChatLogsEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override AdminNanoChatLogsEuiState GetNewState()
    {
        var station = _entMan.EntityQueryEnumerator<StationNanoChatLogsComponent>();
        var logs = new List<AdminNanoChatLogEntry>();

        while (station.MoveNext(out _, out var stationLogs))
        {
            foreach (var log in stationLogs.Logs)
                logs.Add(log);
        }

        return new AdminNanoChatLogsEuiState(logs);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AdminNanoChatLogsEuiMsg.RefreshLogs:
                {
                    StateDirty();
                    break;
                }
        }
    }
}
