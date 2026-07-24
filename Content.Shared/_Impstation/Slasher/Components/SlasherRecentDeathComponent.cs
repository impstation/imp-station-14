using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Tracks when a humanoid most recently entered dead state for Slasher hook target gating.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SlasherRecentDeathComponent : Component
{
    /// <summary>
    /// Timestamp of the most recent transition into dead state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? TimeOfDeath;

    /// <summary>
    /// Maximum age of a corpse that is still valid for meathook insertion.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan RecentDeathWindow = TimeSpan.FromMinutes(3);
}
