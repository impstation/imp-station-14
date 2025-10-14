//using Content.Shared.EntityTable; // imp unused
using Content.Shared.Maps;
//using Content.Shared.Storage; // imp unused
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places tiles / entities onto room entrances.
/// </summary>
public sealed partial class RoomEntranceDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField]
    public EntProtoId Contents; // imp, was entitytable
}
