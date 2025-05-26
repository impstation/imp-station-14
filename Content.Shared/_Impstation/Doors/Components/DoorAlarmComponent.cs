using Content.Shared.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles bolt-specific behavior.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoorSystem))]
[AutoGenerateComponentState]
public sealed partial class DoorAlarmComponent : Component
{
    /// <summary>
    /// Whether the alarm is currently tripped
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlarmTripped = false;

    /// <summary>
    /// True if the Alarm wire is cut, which will disable it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlarmWireCut;

}
