using Content.Client.Eui;
using JetBrains.Annotations;

namespace Content.Client._Impstation.Heretic.UI;

[UsedImplicitly]
public sealed class HellMemoryEui : BaseEui
{
    private HellMemoryMenu Menu { get; }

    public HellMemoryEui()
    {
        Menu = new HellMemoryMenu();
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
