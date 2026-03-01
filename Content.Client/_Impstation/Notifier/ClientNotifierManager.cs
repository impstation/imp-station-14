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

    public void UpdateNotifier(PlayerNotifierSettings notifierSettings)
    {
        var msg = new MsgUpdateNotifier
        {
            Notifier = notifierSettings,
        };
        _sawmill?.Debug($"keb toy:'{msg.Notifier.Freetext}' pib '{msg.Notifier.Enabled}'");
        _netManager.ClientSendMessage(msg);
    }

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
