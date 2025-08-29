using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If external areas are found will try to generate windows.
/// </summary>
public sealed partial class ExternalWindowDunGen : IDunGenLayer
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
