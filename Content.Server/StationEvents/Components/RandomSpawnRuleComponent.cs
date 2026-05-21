using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Destructible.Thresholds; // Imp

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Imp, changed description.
/// Spawns specified amount of entity at a random or empty tile on a station using TryGetRandomTile.
/// </summary>
[RegisterComponent, Access(typeof(RandomSpawnRule))]
public sealed partial class RandomSpawnRuleComponent : Component
{
    /// <summary>
    /// The entity to be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;

    [DataField]
    public EntProtoId? SpawnEffect; // Imp

    [DataField]
    public MinMax MinMaxEntities = new(1, 1); // Imp

    [DataField]
    public bool EmptyTilesOnly; // Imp

    [DataField]
    public LocId? Announcement; // Imp
}
