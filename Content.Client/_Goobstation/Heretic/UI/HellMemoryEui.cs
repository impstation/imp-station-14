using Content.Client.Eui;
using Content.Shared._Goobstation.Heretic.UI;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Goobstation.Heretic.UI;

[UsedImplicitly]
public sealed class HellMemoryEui : BaseEui
{
    private HellMemoryMenu Menu { get; }

    public HellMemoryEui()
    {
        Menu = new HellMemoryMenu();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not HellMemoryEuiState s)
            return;

        Menu.SetMessage(s.Message);
    }

    public override void Opened()
    {
        Menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        Menu.Close();
    }
}
