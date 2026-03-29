using Content.Shared._Impstation.Notifier;
using Robust.Shared.Network;

namespace Content.Client._Impstation.Notifier;

public sealed class ClientNotifierManager : IClientNotifierManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill? _sawmill = null;

    private PlayerNotifierSettings? _notifier;

    public event Action? OnServerDataLoaded;

    public bool HasLoaded => _notifier is not null;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("clientnotifier");
        _netManager.RegisterNetMessage<MsgUpdateNotifier>(HandleUpdateNotifier);

    }

    /// <summary>
    /// Send updated player notifier settings to server.
    /// </summary>
    /// <param name="notifierSettings"></param>
    public void UpdateNotifier(PlayerNotifierSettings notifierSettings)
    {
        var msg = new MsgUpdateNotifier
        {
            Notifier = notifierSettings,
        };
        _netManager.ClientSendMessage(msg);
    }
/// <summary>
/// Get the currently stored notifier system, throw an error if accessed before loaded.
/// </summary>
/// <returns></returns>
/// <exception cref="InvalidOperationException"></exception>
    public PlayerNotifierSettings GetNotifier()
    {
        if (_notifier is null)
            throw new InvalidOperationException("Notifier settings not loaded yet?");

        return _notifier;
    }

    private void HandleUpdateNotifier(MsgUpdateNotifier message)
    {
        _notifier = message.Notifier;

        OnServerDataLoaded?.Invoke();
    }
}
