using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist.Components;

/// <summary>
/// photograph objective component
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTouristCameraSystem))]
public sealed partial class PhotographObjectObjectiveComponent : Component
{
    /// <summary>
    /// List of prototype IDs that are valid for the objective
    /// </summary>
    [DataField("targets")]
    public List<ProtoId<EntityPrototype>> TargetPrototypes = new();
}
