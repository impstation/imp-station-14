using Content.Shared._Impstation.Captain;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Captain;

[UsedImplicitly]
public sealed class CaptainSwordMenuBoundUserInterface : BoundUserInterface
{
    private CaptainSwordMenu? _window;

    public CaptainSwordMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CaptainSwordMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CaptainSwordMenuBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new CaptainSwordChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new CaptainSwordMenuApproveMessage());
    }
}
