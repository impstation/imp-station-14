using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
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
public sealed class NotifierWindowUIController : UIController, IOnSystemChanged<NotifierSystem>, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private readonly IInputManager _input = default!;
    private NotifierWindow? _window;

    private MenuButton? NotifierButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.NotifierButton;
    private Button? LobbyNotifierButton => (UIManager.ActiveScreen as LobbyGui)?.NotifierButton;

    /// <summary>
    /// When the notifier exists, set up the window and input command
    /// </summary>
    /// <param name="system"></param>
    public void OnSystemLoaded(NotifierSystem system)
    {
        EnsureWindow();
        _input.SetInputCommand(ContentKeyFunctions.OpenNotifierWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
    }

    public void OnSystemUnloaded(NotifierSystem system)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }

    /// <summary>
    /// Activate the button in lobby
    /// </summary>
    /// <param name="state"></param>
    public void OnStateEntered(LobbyState state)
    {
        EnsureWindow();
        if (LobbyNotifierButton == null)
            return;

        LobbyNotifierButton.OnPressed += NotifierButtonPressed;
    }

    public void OnStateExited(LobbyState state)
    {
        if (LobbyNotifierButton != null)
            LobbyNotifierButton.OnPressed -= NotifierButtonPressed;
    }

    /// <summary>
    /// When we enter the game, make sure the buttons work.
    /// </summary>
    /// <param name="state"></param>
    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();
        if (NotifierButton == null)
            return;
        NotifierButton.OnPressed -= NotifierButtonPressed;
        NotifierButton.OnPressed += NotifierButtonPressed;
    }

    /// <summary>
    /// Dispose of it when we're done.
    /// </summary>
    /// <param name="state"></param>
    public void OnStateExited(GameplayState state)
    {
        if (NotifierButton == null)
            return;
        NotifierButton.OnPressed -= NotifierButtonPressed;
    }

    /// <summary>
    /// Remove the event from the button
    /// </summary>
    public void UnloadButton()
    {
        if (NotifierButton == null)
            return;
        NotifierButton.OnPressed -= NotifierButtonPressed;

        if (LobbyNotifierButton == null)
            return;
        LobbyNotifierButton.OnPressed -= NotifierButtonPressed;
    }

    /// <summary>
    /// Add the event to the button
    /// </summary>
    public void LoadButton()
    {
        if (NotifierButton == null)
            return;
        NotifierButton.OnPressed += NotifierButtonPressed;

        if (LobbyNotifierButton == null)
            return;
        LobbyNotifierButton.OnPressed += NotifierButtonPressed;
    }

    /// <summary>
    /// Toggle the window when the button is pressed.
    /// </summary>
    /// <param name="args"></param>
    private void NotifierButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    /// <summary>
    /// Ensure the window actually exists and works with the buttons
    /// </summary>
    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<NotifierWindow>();
        _window.OnOpen += () =>{
            SetNotifierPressed(true);
        };
        _window.OnClose += () =>{
            SetNotifierPressed(false);
            _window.UpdateUi();
        };
        SetNotifierPressed(_window.IsOpen);
    }

    /// <summary>
    /// Open or close the window.
    /// </summary>
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

    private void SetNotifierPressed(bool pressed)
    {
        if (NotifierButton != null)
        {
            NotifierButton.Pressed = pressed;
        }

        if (LobbyNotifierButton != null)
        {
            LobbyNotifierButton.Pressed = pressed;
        }
    }
}
