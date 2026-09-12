using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Destructible.Thresholds; // Imp
using Content.Shared.Radio; // Moffstation - Syndicate dead drop

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

    /// <summary>
    /// Imp.
    /// Variation in the amount of entities to spawn.
    /// </summary>
    [DataField]
    public MinMax MinMaxEntities = new(1, 1);

    /// <summary>
    /// Imp.
    /// Whether to spawn entities on tiles without dynamic or static entities.
    /// </summary>
    [DataField]
    public bool EmptyTilesOnly;

    /// <summary>
    /// Imp.
    /// Announcement to be played when a station event with this rule is added.
    /// </summary>
    [DataField]
    public LocId? Announcement;

    // Moffstation - Start - Syndicate dead drop
    /// <summary>
    /// The radio message to send when spawning the entity. The entity is used as the sender of the radio message.
    /// </summary>
    [DataField]
    public LocId? RadioMessage; // Imp, made into a LocId over Moff RandomSpawnRuleRadioMessage
    // Moffstation - End

    /// <summary>
    /// Imp.
    /// Radio channel to send the message over, moved from Moff RandomSpawnRuleRadioMessage
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Common";
}
