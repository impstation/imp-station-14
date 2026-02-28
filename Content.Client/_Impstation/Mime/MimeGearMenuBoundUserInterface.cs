using Content.Shared._Impstation.Mime;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Mime;

[UsedImplicitly]
public sealed class MimeGearMenuBoundUserInterface : BoundUserInterface
{
    private MimeGearMenu? _window;

    public MimeGearMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MimeGearMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MimeGearMenuBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new MimeGearChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new MimeGearMenuApproveMessage());
    }
}
