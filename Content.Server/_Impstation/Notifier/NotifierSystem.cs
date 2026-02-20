using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Impstation.Notifier;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.Notifier;

public sealed class NotifierSystem : SharedNotifierSystem
{
    private readonly Dictionary<NetUserId, PlayerNotifierSettings> NotifierUsers= new();
    protected override string GetNotifierText(NetUserId userId)
    {
        TryGetNotifier(userId, out var notifier);
        var text = notifier?.Freetext ?? string.Empty;

        if (text == string.Empty)
            text = Loc.GetString("");

        return text;
    }
    public override void SetPlayerNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        base.SetPlayerNotifier(userId, notifierSettings);

        if (notifierSettings == null)
        {
            return;
        }

        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        if (exists)
            notifierComponent!.Settings = notifierSettings;
    }

    protected override bool GetNotifierEnabled(NetUserId userId)
    {
        TryGetNotifier(userId, out var notifier);
        return notifier?.Enabled ?? false;
    }

    public bool TryAddNotifier(NetUserId userId, PlayerNotifierSettings notifier)
    {
        var success = NotifierUsers.TryAdd(userId, notifier);
        return success;
    }
    public bool TrySetNotifier(NetUserId userId, PlayerNotifierSettings notifier)
    {
        if (NotifierUsers.ContainsKey(userId))
        {
            NotifierUsers[userId] = notifier;
            return true;
        }
        return false;
    }
    private bool TryGetNotifier(NetUserId userId, [NotNullWhen(true)] out PlayerNotifierSettings? notifierSettings)
    {
        var exists = NotifierUsers.TryGetValue(userId, out notifierSettings);
        return exists;
    }
}
