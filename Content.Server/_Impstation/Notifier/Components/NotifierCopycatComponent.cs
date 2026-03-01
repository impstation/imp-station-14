using Content.Shared._Impstation.Notifier;

namespace Content.Server._Impstation.Notifier.Components;

[RegisterComponent]
public sealed partial class NotifierCopycatComponent : Component
{
    public Dictionary<EntityUid, PlayerNotifierSettings> Copies = new();
}
