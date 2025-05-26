using Content.Shared.Doors.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    public void SetAlarmTripped(Entity<DoorAlarmComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        TrySetAlarmTripped(ent, value, user, predicted);
    }

    public bool TrySetAlarmTripped(
        Entity<DoorAlarmComponent> ent,
        bool value,
        EntityUid? user = null,
        bool predicted = false
    )
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return false;
        if (ent.Comp.AlarmTripped == value)
            return false;

        ent.Comp.AlarmTripped = value;
        Dirty(ent, ent.Comp);
        return true;
    }

    public bool IsAlarmTripped(EntityUid uid, DoorAlarmComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return false;
        }

        return component.AlarmTripped;
    }
}
