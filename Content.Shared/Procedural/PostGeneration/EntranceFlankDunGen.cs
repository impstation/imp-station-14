using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities on either side of an entrance.
/// </summary>
public sealed partial class EntranceFlankDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    // Imp Edit Start
    /// <summary>
    ///     All the entities that will be placed on the tile.
    ///     Imp: used to be an EntityTable, now a list of Entities.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<EntityPrototype>> Contents = [];

    /// <summary>
    ///     Do we want to use a random entity from the list?
    /// </summary>
    [DataField]
    public bool UseRandomEntity;
    // Imp Edit End
}
