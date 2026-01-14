using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Heretic.UI;

[Serializable, NetSerializable]
public sealed class HellMemoryEuiState : EuiStateBase
{
    public string Message { get; set; }
    public HellMemoryEuiState(string message)
    {
        Message = message;
    }
}
