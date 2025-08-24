using Content.Server.Objectives.Systems;
using Content.Shared._Impstation.Tourist;

namespace Content.Server._Impstation.Tourist.Components;

/// <summary>
/// Objective condition that requires the player to be a tourist and have photographed at least a random number of their target.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(TouristConditionsSystem), typeof(SharedTouristCameraSystem))]
public sealed partial class TouristPhotosConditionComponent : Component
{
    [DataField("photos"), ViewVariables(VVAccess.ReadWrite)]
    public int Photos;

    /// <summary>
    /// Prototypes that will contribute to the objective
    /// </summary>
    [DataField("targetPrototypes")]
    public HashSet<string> TargetPrototypes = new();
}
