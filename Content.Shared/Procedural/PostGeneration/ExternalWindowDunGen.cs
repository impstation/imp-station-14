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
