using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;

namespace Content.Client._Impstation.Notifier.UI;
[UsedImplicitly]
public sealed class NotifierWindowUIController :UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IInputManager _input = default!;
    private NotifierWindow? _window;

    private MenuButton? NotifierButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.NotifierButton;

    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();

        _input.SetInputCommand(ContentKeyFunctions.OpenNotifierWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }
    public void UnloadButton()
    {
        if (NotifierButton == null)
        {
            return;
        }

        NotifierButton.OnPressed -= NotifierButtonPressed;
    }

    public void LoadButton()
    {
        if (NotifierButton == null)
        {
            return;
        }

        NotifierButton.OnPressed += NotifierButtonPressed;
    }

    private void NotifierButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }


    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<NotifierWindow>();
        _window.OnOpen += () => {
            if (NotifierButton is not null)
                NotifierButton.Pressed = true;
        };
        _window.OnClose += () => {
            if (NotifierButton is not null)
                NotifierButton.Pressed = false;
//            _window.UpdateUi(); // Discard unsaved changes
        };
    }

    private void ToggleWindow()
    {
        if (_window is null)
            return;

        UIManager.ClickSound();
        if (_window.IsOpen != true)
        {
            _window.OpenCentered();
        }
        else
        {
            _window.Close();
        }
    }
}
