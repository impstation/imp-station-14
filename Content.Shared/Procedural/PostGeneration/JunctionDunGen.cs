//using Content.Shared.EntityTable; // imp unused
using Content.Shared.Maps;
//using Content.Shared.Storage; // imp unused
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places the specified entities at junction areas.
/// </summary>
public sealed partial class JunctionDunGen : IDunGenLayer
{
    /// <summary>
    /// Width to check for junctions.
    /// </summary>
    [DataField]
    public int Width = 3;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    // imp: changed from ent table to ent. and added summary
    /// <summary>
    ///     The Entity that will be placed on this tile
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Contents; // imp
}
