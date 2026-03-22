namespace Content.Shared._Impstation.Notifier.events;

public sealed class NotifierCopycatUpdateEvent : EntityEventArgs
{
    public PlayerNotifierSettings Settings { get; set; } = new PlayerNotifierSettings();
}
