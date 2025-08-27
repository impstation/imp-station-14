using Content.Server.EUI;

namespace Content.Server._Goobstation.Heretic.UI;

public sealed class MinionNotifEui : BaseEui
{
    public MinionNotifEui()
    {
        IoCManager.InjectDependencies(this);
    }
    public override void Opened()
    {
        StateDirty();
    }
}
