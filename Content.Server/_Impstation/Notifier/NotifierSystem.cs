using Content.Shared._Impstation.Notifier;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.Notifier;

public sealed class NotifierSystem : SharedNotifierSystem
{
    protected override FormattedMessage GetNotifierText(NetUserId userId)
    {
        TryGetNotifier(userId, out var notifier);
        var text = notifier?.Freetext ?? string.Empty;

        if (text == string.Empty)
            text = Loc.GetString("");

        var message = new FormattedMessage();
        message.AddText(text);

        return message;
    }
    public override void SetNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        if (notifierSettings == null)
        {
            UserNotifiers.Remove(userId);
            return;
        }

        UserNotifiers[userId] = notifierSettings;
    }

    protected override bool GetNotifierEnabled(NetUserId userId)
    {
        TryGetNotifier(userId, out var notifier);
        return notifier?.Enabled ?? false;
    }
}
