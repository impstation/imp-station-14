using Content.Server.EUI;
using Content.Shared._Goobstation.Heretic.UI;
using Content.Shared.Eui;

namespace Content.Server._Goobstation.Heretic.UI;

public sealed class HellMemoryEui : BaseEui
{
    private string Message { get; set; } = string.Empty;

    public HellMemoryEui(string message)
    {
        Message = message;
    }

    public override EuiStateBase GetNewState()
    {
        return new HellMemoryEuiState(Message);
    }
}
