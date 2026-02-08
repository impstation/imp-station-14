using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Exists purely for the ability for Incubators to keep track of what they're doing
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class EggIncubatorComponent : Component
{
    /// <summary>
    /// When do we finish incubation?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan FinishIncubation = TimeSpan.Zero;

    /// <summary>
    /// Egg we are currently incubating
    /// </summary>
    public IncubationComponent CurrentlyIncubated;

    /// <summary>
    /// Used for tracking visuals
    /// </summary>
    public IncubatorStatus Status;
}

[Serializable, NetSerializable]
public enum IncubatorVisualizerLayers : byte
{
    Status
}

[Serializable, NetSerializable]
public enum IncubatorStatus : byte
{
    Active,
    Inactive
}
