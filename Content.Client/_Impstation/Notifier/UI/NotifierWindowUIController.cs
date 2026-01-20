using Content.Client.Gameplay;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Impstation.Notifier.UI;

public sealed class NotifierWindowUIController :UIController, IOnStateChanged<GameplayState>
{
    public void OnStateEntered(GameplayState state)
    {
        throw new NotImplementedException();
    }

    public void OnStateExited(GameplayState state)
    {
        throw new NotImplementedException();
    }
}
