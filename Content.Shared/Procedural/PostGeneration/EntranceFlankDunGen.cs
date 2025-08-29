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
    // All the entities that will be placed on the tile
    [DataField(required: true)]
    public List<ProtoId<EntityPrototype>> Contents = new List<ProtoId<EntityPrototype>>(); 

    //Do we want to use a random entity from the list?
    [DataField]
    public bool useRandomEntity;
    // Imp Edit End
}
