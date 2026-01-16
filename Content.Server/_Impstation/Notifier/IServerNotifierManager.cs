using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Impstation.Notifier;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Notifier;

public interface IServerNotifierManager
{
    void Initialize();

    Task LoadData(ICommonSession session, CancellationToken cancel);
    void OnClientDisconnected(ICommonSession session);

    /// <summary>
    /// Get player notifier settings
    /// </summary>
    PlayerNotifierSettings GetPlayerConsentSettings(NetUserId userId);
}
