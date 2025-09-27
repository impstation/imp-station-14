using Content.Shared._Impstation.Service;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Service;

[UsedImplicitly]
public sealed class ServiceJobBoardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ServiceJobBoardMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ServiceJobBoardMenu>();

        _menu.OnSelectButtonPressed += id =>
            SendMessage(new ServiceJobBoardSelectMessage(id));
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not ServiceJobBoardConsoleState state)
            return;

        _menu?.Update(state);
    }
}
