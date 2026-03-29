using Content.Shared._Impstation.Notifier;

namespace Content.Client._Impstation.Notifier;

public interface IClientNotifierManager
{
    event Action OnServerDataLoaded;
    bool HasLoaded { get; }

    void Initialize();
    void UpdateNotifier(PlayerNotifierSettings consentSettings);
    PlayerNotifierSettings GetNotifier();
}
