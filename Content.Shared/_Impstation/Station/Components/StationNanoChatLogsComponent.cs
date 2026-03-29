using Content.Shared._Impstation.NanoChat;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Station.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StationNanoChatLogsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<AdminNanoChatLogEntry> Logs = [];
}
