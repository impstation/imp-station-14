using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Spawns a protoype when triggered.
/// If TargetUser is true it will be spawned at their location.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpawnOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The prototype to spawn.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Proto = string.Empty;

    /// <summary>
    /// Use MapCoordinates for spawning?
    /// Set to true if you don't want the new entity parented to the spawner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseMapCoords;

    /// <summary>
    /// Whether or not to use predicted spawning.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Predicted;

    /// <summary>
    ///     #IMP Allows multiple entities to be spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Amount = 1;

    /// <summary>
    ///     #IMP Amount reduces by one for every entity spawned.
    ///     If SingleUse is set to false, this will be reset after all entities spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SingleUse = true;
}
