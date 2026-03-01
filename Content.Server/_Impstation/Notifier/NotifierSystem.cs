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
    private readonly Dictionary<NetUserId, PlayerNotifierSettings> NotifierUsers= new();// holds the notifier settings for current players, we don't get directly from the DB because those are async operations.

    /// <summary>
    /// Get a users freetext from their settings.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <returns></returns>
    protected override string GetNotifierText(NetUserId userId)
    {
        TryGetServerNotifier(userId, out var notifier);
        var text = notifier?.Freetext ?? string.Empty;

        if (text == string.Empty)
            text = Loc.GetString("");

        return text;
    }

    /// <summary>
    ///  Set the notifier settings of an entity attached to the given user ID. If null, will remove the entry from the currently loaded notifiers.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <param name="notifierSettings"> The players notifier settings, includes the freetext and whether its enabled</param>
    /// <returns></returns>
    public override void SetPlayerNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        base.SetPlayerNotifier(userId, notifierSettings);

        if (notifierSettings == null)// check if the given settings are null, we assume this means that the player has disconnected.
        {
            NotifierUsers.Remove(userId);
            return;
        }

        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        if (exists)
            notifierComponent!.Settings = notifierSettings;
    }

    /// <summary>
    /// Check if the user has the notifier enabled.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <returns></returns>
    protected override bool GetNotifierEnabled(NetUserId userId)
    {
        TryGetServerNotifier(userId, out var notifier);
        return notifier?.Enabled ?? false;
    }

    /// <summary>
    /// Add a player to the currently loaded notifiers.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <param name="notifier">The players notifier settings, includes the freetext and whether its enabled.</param>
    /// <returns></returns>
    public bool TryAddNotifier(NetUserId userId, PlayerNotifierSettings notifier)
    {
        var success = NotifierUsers.TryAdd(userId, notifier);
        return success;
    }
    /// <summary>
    /// Attempt to set the value of the players notifier in the serverside dictionary.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <param name="notifier">The players notifier settings, includes the freetext and whether its enabled.</param>
    /// <returns></returns>
    public bool TrySetServerNotifiers(NetUserId userId, PlayerNotifierSettings notifier)
    {
        if (NotifierUsers.ContainsKey(userId))
        {
            NotifierUsers[userId] = notifier;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Try to get a players notifier from the dictionary.
    /// </summary>
    /// <param name="userId">The target players user id.</param>
    /// <param name="notifier">The players notifier settings, includes the freetext and whether its enabled.</param>
    /// <returns></returns>
    private bool TryGetServerNotifier(NetUserId userId, [NotNullWhen(true)] out PlayerNotifierSettings? notifierSettings)
    {
        var exists = NotifierUsers.TryGetValue(userId, out notifierSettings);
        return exists;
    }
}
