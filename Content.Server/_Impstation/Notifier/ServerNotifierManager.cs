using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Impstation.Notifier;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Notifier;

public sealed class ServerNotifierManager : IServerNotifierManager
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill? _sawmill = null;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("serverconsent");
        _netManager.RegisterNetMessage<MsgUpdateNotifier>(HandleUpdateNotifierMessage);

    }
    private async void HandleUpdateNotifierMessage(MsgUpdateNotifier message)
    {
        var userId = message.MsgChannel.UserId;
        var notifierSystem = _entityManager.System<SharedNotifierSystem>();

        if (!notifierSystem.TryGetNotifier(userId, out _))
            return;

        notifierSystem.SetNotifier(userId, message.Notifier);

        var session = _playerManager.GetSessionByChannel(message.MsgChannel);

        if (ShouldStoreInDb(message.MsgChannel.AuthType))
            await _db.SavePlayerNotifierSettingsAsync(userId, message.Notifier);

        // send it back to confirm to client that consent was updated
        _netManager.ServerSendMessage(message, message.MsgChannel);
    }

    public Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    public void OnClientDisconnected(ICommonSession session)
    {
        throw new NotImplementedException();
    }

    public PlayerNotifierSettings GetPlayerConsentSettings(NetUserId userId)
    {
        throw new NotImplementedException();
    }

    private static bool ShouldStoreInDb(LoginType loginType)
    {
        return loginType.HasStaticUserId();
    }
}
