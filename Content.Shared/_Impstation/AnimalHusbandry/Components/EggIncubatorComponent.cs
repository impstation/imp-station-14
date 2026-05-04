using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
///     An entity that will incubate its contents that have <see cref="IncubationComponent"/>.
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
