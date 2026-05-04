using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Exists purely for the ability for Incubators to keep track of what they're doing
/// </summary>
[RegisterComponent]
public sealed partial class EggIncubatorComponent : Component
{
    /// <summary>
    ///     The ID of the container used to incubate its contents.
    /// </summary>
    [DataField]
    public string ContainerId = "egg";
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
