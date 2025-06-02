using Content.Shared.Alert;

namespace Content.Shared._RMC14.NightVision;

[DataDefinition]
public sealed partial class ToggleNightVisionAlertEvent : BaseAlertEvent
{
    public EntityUid User { get; }
    public bool Active { get; }

    public ToggleNightVisionAlertEvent(EntityUid user, bool active)
    {
        User = user;
        Active = active;
    }
}