using Robust.Shared.Audio;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Plays periodic heartbeat audio while a Slasher remains in combat mode.
/// </summary>
[RegisterComponent, Access(typeof(SlasherCombatHeartbeatSystem))]
public sealed partial class SlasherCombatHeartbeatComponent : Component
{
    /// <summary>
    /// Time between heartbeat sounds while combat mode remains active.
    /// </summary>
    [DataField]
    public TimeSpan BeatInterval = TimeSpan.FromSeconds(1.2);

    /// <summary>
    /// Heartbeat sound played around the Slasher while in combat mode.
    /// </summary>
    [DataField]
    public SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/heartbeat.ogg");

    /// <summary>
    /// Next scheduled heartbeat timestamp; beats fire when current time reaches this value.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextBeatTime;
}
