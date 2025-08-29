using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="EntranceFlankDunGen"/>
    /// </summary>
    private async Task PostGen(EntranceFlankDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var tiles = new List<(Vector2i Index, Tile)>();
        var tileDef = _tileDefManager[gen.Tile];
        var spawnPositions = new ValueList<Vector2i>(dungeon.Rooms.Count);
        var contents = gen.Contents; // imp

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                for (var i = 0; i < 8; i++)
                {
                    var dir = (Direction) i;
                    var neighbor = entrance + dir.ToIntVec();

                    if (!dungeon.RoomExteriorTiles.Contains(neighbor))
                        continue;

                    if (reservedTiles.Contains(neighbor))
                        continue;

                    tiles.Add((neighbor, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
                    spawnPositions.Add(neighbor);
                }
            }
        }

        _maps.SetTiles(_gridUid, _grid, tiles);

        // Places the entity in the valid location. If randomisation is chosen, then it will only place one random entity from the list. also // Imp
        foreach (var entrance in spawnPositions)
        {
            // Imp Edit Start
            if (!gen.useRandomEntity)
            {
                foreach (var entity in contents)
                {
                    _entManager.SpawnEntity(entity, _maps.GridTileToLocal(_gridUid, _grid, entrance));
                }
            }
            else
            {
                _entManager.SpawnEntity(contents[random.Next(0, gen.Contents.Count)], _maps.GridTileToLocal(_gridUid, _grid, entrance));
            }
            // Imp Edit End
        }
    }
}
