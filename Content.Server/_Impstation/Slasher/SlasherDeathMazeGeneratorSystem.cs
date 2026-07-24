using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server._Impstation.Slasher.Components;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Controls how death-maze spawn selection filters marker candidates.
/// </summary>
public enum DeathMazeSpawnSelection : byte
{
    Any,
    CenterOnly,
    NonCenterOnly,
}

/// <summary>
/// Handles procedural generation and environment setup for the Slasher death maze.
/// Responsible for creating the maze grid, carving rooms and corridors, placing walls,
/// and spawning fill markers. Consumed by <see cref="SlasherDeathTeleportSystem"/>.
/// </summary>
public sealed class SlasherDeathMazeGeneratorSystem : EntitySystem
{
    private const int DeathMazeCanvasHalfSize = 160;
    private const int DeathMazeMaxRoomSize = 5;
    private const int DeathMazeMinRoomSize = 3;
    private const string DeathMazeRoomFillMarkerPrototype = "SlasherDeathMazeRoomFillMarker";

    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;

    private static readonly Vector2i[] CardinalDirections =
    [
        new Vector2i(1, 0),
        new Vector2i(-1, 0),
        new Vector2i(0, 1),
        new Vector2i(0, -1),
    ];

    private readonly record struct MazeRoomFootprint(Vector2i Min, Vector2i Max)
    {
        public int Width => Max.X - Min.X + 1;
        public int Height => Max.Y - Min.Y + 1;
        public Vector2i Center => new((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);
    }

    /// <summary>
    /// Creates a fresh death-maze map and grid and returns them.
    /// Configures the map environment and seeds the tile canvas before returning.
    /// </summary>
    public bool TryCreateDeathMazeGrid(out EntityUid gridUid, out MapGridComponent gridComp)
    {
        gridUid = default;
        gridComp = default!;

        _map.CreateMap(out var mapId);
        var mapUid = _map.GetMapOrInvalid(mapId);
        ConfigureMapEnvironment(mapUid);

        var newGridEnt = _mapManager.CreateGridEntity(mapId);
        gridUid = newGridEnt.Owner;
        gridComp = newGridEnt.Comp;

        CreateCanvas(gridUid, gridComp);
        return true;
    }

    /// <summary>
    /// Carves the full room-and-corridor layout into an existing grid canvas
    /// and returns the set of hallway tiles for later spawn sampling.
    /// </summary>
    public HashSet<Vector2i> GenerateHallways(
        EntityUid gridUid,
        MapGridComponent gridComp,
        SlasherRuleComponent rule,
        Vector2i centerTile)
    {
        var carved = new HashSet<Vector2i>();
        var hallwayTiles = new HashSet<Vector2i>();
        var rooms = new List<MazeRoomFootprint>();
        var lineCount = Math.Max(0, rule.DeathMazeSampleLineCount);
        var lineLength = Math.Max(1, rule.DeathMazeSampleLineLength);
        var roomFloorDef = (ContentTileDefinition)_tileDefs["FloorSteel"];
        var hallwayFloorDef = (ContentTileDefinition)_tileDefs["Plating"];
        var perimeterHalfSize = GetPerimeterHalfSize(lineLength);

        var startRoom = CreateCenteredRoom(centerTile, PickRoomSize(), PickRoomSize());
        if (!IsRoomInsidePerimeterBounds(startRoom, perimeterHalfSize))
            startRoom = CreateCenteredRoom(centerTile, DeathMazeMinRoomSize, DeathMazeMinRoomSize);

        CarveRoom(carved, startRoom);
        rooms.Add(startRoom);

        if (lineCount > 0)
        {
            var desiredRooms = Math.Max(12, lineCount);
            var minCorridorLength = Math.Max(6, lineLength / 8);
            var maxCorridorLength = Math.Max(minCorridorLength + 4, lineLength / 4);
            var attempts = desiredRooms * 12;

            for (var i = 0; i < attempts && rooms.Count < desiredRooms; i++)
            {
                TryAddRoom(carved, hallwayTiles, rooms, perimeterHalfSize, minCorridorLength, maxCorridorLength);
            }
        }

        FinalizeGeometry(gridUid, gridComp, carved, hallwayTiles, roomFloorDef, hallwayFloorDef);
        SpawnRoomFillMarkers(gridUid, gridComp, rooms);
        EnsureAccessibleOrigin(gridUid, gridComp, roomFloorDef);

        // Rebuild atmosphere so carved tiles get breathable air
        if (TryComp<GridAtmosphereComponent>(gridUid, out var gridAtmos))
            _atmosphere.RebuildGridAtmosphere((gridUid, gridAtmos, gridComp));

        return hallwayTiles;
    }

    /// <summary>
    /// Samples hallway tiles for spawn fallback selection, intersecting with reachable tiles if cached.
    /// Falls back to radial line casting if no cache is available.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="rule">The active Slasher rule component for sampling configuration.</param>
    /// <param name="cachedHallwayTiles">Optional cached hallway tiles from a previous generation.</param>
    /// <returns>Set of sampled hallway tiles suitable for spawn placement.</returns>
    public HashSet<Vector2i> GetSampledHallwayFallbackTiles(
        EntityUid gridUid,
        MapGridComponent gridComp,
        HashSet<Vector2i> reachableTiles,
        Vector2i centerTile,
        SlasherRuleComponent rule,
        HashSet<Vector2i>? cachedHallwayTiles)
    {
        if (cachedHallwayTiles != null && cachedHallwayTiles.Count > 0)
        {
            var cachedReachable = new HashSet<Vector2i>();
            foreach (var tile in cachedHallwayTiles)
            {
                if (reachableTiles.Contains(tile))
                    cachedReachable.Add(tile);
            }

            return cachedReachable;
        }

        return BuildSampledHallwayFallbackTiles(gridUid, gridComp, reachableTiles, centerTile, rule);
    }

    /// <summary>
    /// Returns whether a tile can be used as a valid death-maze path or spawn tile.
    /// Checks for empty tiles, space, and collision blocking.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="tile">The tile coordinate to validate.</param>
    /// <returns>True if the tile is walkable and usable for spawns or paths.</returns>
    public bool IsValidTile(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef)
            || tileRef.Tile.IsEmpty
            || _turf.IsSpace(tileRef)
            || _turf.IsTileBlocked(tileRef, CollisionGroup.MobMask))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes all non-protected entities and clears all tiles on the given grid,
    /// returning it to a blank canvas for regeneration.
    /// </summary>
    /// <param name="gridUid">The grid entity to normalize.</param>
    /// <param name="gridComp">The grid component.</param>
    public void NormalizeCanvas(EntityUid gridUid, MapGridComponent gridComp)
    {
        var entQuery = EntityQueryEnumerator<TransformComponent>();
        while (entQuery.MoveNext(out var uid, out var xform))
        {
            if (uid == gridUid || xform.GridUid != gridUid || IsProtectedEntity(uid))
                continue;

            QueueDel(uid);
        }

        var localAabb = gridComp.LocalAABB;
        var minX = (int)MathF.Floor(localAabb.Left) - 1;
        var maxX = (int)MathF.Ceiling(localAabb.Right) + 1;
        var minY = (int)MathF.Floor(localAabb.Bottom) - 1;
        var maxY = (int)MathF.Ceiling(localAabb.Top) + 1;

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var tile = new Vector2i(x, y);
                if (_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef) && !tileRef.Tile.IsEmpty)
                    _map.SetTile(gridUid, gridComp, tile, Tile.Empty);
            }
        }
    }

    /// <summary>
    /// Returns true if the entity is a protected death-maze marker that should
    /// survive canvas normalization or hallway carving.
    /// </summary>
    /// <param name="uid">The entity to test.</param>
    /// <returns>True if the entity is a protected death-maze marker.</returns>
    public bool IsProtectedEntity(EntityUid uid)
    {
        return HasComp<DeathMazeSpawnLocationComponent>(uid)
               || HasComp<SlasherBossSpawnerComponent>(uid)
               || HasComp<SlasherRiftTeleportComponent>(uid);
    }

    /// <summary>
    /// Configures gravity and atmosphere for a new death-maze map.
    /// Sets inherent gravity and establishes breathable air mixture.
    /// </summary>
    /// <param name="mapUid">The map entity to configure.</param>
    private void ConfigureMapEnvironment(EntityUid mapUid)
    {
        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(mapUid, gravity);

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int)Gas.Oxygen] = 21.824779f;
        moles[(int)Gas.Nitrogen] = 82.10312f;
        var mixture = new GasMixture(moles, Atmospherics.T20C);
        _atmosphere.SetMapAtmosphere(mapUid, false, mixture);
    }

    /// <summary>
    /// Seeds a new grid canvas with empty tiles for procedural maze carving.
    /// Initializes atmosphere for the grid.
    /// </summary>
    /// <param name="gridUid">The grid entity to seed.</param>
    /// <param name="gridComp">The grid component.</param>
    private void CreateCanvas(EntityUid gridUid, MapGridComponent gridComp)
    {
        var tiles = new List<(Vector2i, Tile)>((DeathMazeCanvasHalfSize * 2 + 1) * (DeathMazeCanvasHalfSize * 2 + 1));
        for (var x = -DeathMazeCanvasHalfSize; x <= DeathMazeCanvasHalfSize; x++)
        {
            for (var y = -DeathMazeCanvasHalfSize; y <= DeathMazeCanvasHalfSize; y++)
            {
                tiles.Add((new Vector2i(x, y), Tile.Empty));
            }
        }

        _map.SetTiles(gridUid, gridComp, tiles);

        var atmosComp = TryComp<GridAtmosphereComponent>(gridUid, out var existingAtmos)
            ? existingAtmos
            : AddComp<GridAtmosphereComponent>(gridUid);

        _atmosphere.RebuildGridAtmosphere((gridUid, atmosComp, gridComp));
    }

    /// <summary>
    /// Applies floor tiles to carved spaces and spawns wall entities around the perimeter.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="carved">Set of tiles that were carved (hallways and rooms).</param>
    /// <param name="hallwayTiles">Subset of carved tiles that are hallway-only (not rooms).</param>
    /// <param name="roomFloorDef">Tile definition to use for room floors.</param>
    /// <param name="hallwayFloorDef">Tile definition to use for hallway floors.</param>
    private void FinalizeGeometry(
        EntityUid gridUid,
        MapGridComponent gridComp,
        HashSet<Vector2i> carved,
        HashSet<Vector2i> hallwayTiles,
        ContentTileDefinition roomFloorDef,
        ContentTileDefinition hallwayFloorDef)
    {
        var floorTiles = new List<(Vector2i, Tile)>(carved.Count);
        foreach (var floorTile in carved)
        {
            var floorDef = hallwayTiles.Contains(floorTile) ? hallwayFloorDef : roomFloorDef;
            floorTiles.Add((floorTile, new Tile(floorDef.TileId)));
        }

        _map.SetTiles(gridUid, gridComp, floorTiles);
        ApplyWallRules(gridUid, gridComp, carved, roomFloorDef);
    }

    /// <summary>
    /// Spawns wall entities around the perimeter of carved space.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="carved">Set of tiles that were carved (floor space).</param>
    /// <param name="floorDef">Floor tile definition (used for reference).</param>
    private void ApplyWallRules(
        EntityUid gridUid,
        MapGridComponent gridComp,
        HashSet<Vector2i> carved,
        ContentTileDefinition floorDef)
    {
        var walls = BuildWallShell(carved);
        foreach (var wallTile in walls)
            EnsureWallAtTile(gridUid, gridComp, wallTile, floorDef);
    }

    /// <summary>
    /// Builds a set of wall tiles surrounding all carved floor space.
    /// </summary>
    /// <param name="carved">Set of carved (floor) tiles.</param>
    /// <returns>Set of tile positions where walls should be placed.</returns>
    private HashSet<Vector2i> BuildWallShell(HashSet<Vector2i> carved)
    {
        var walls = new HashSet<Vector2i>();
        foreach (var floorTile in carved)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    var candidate = floorTile + new Vector2i(x, y);
                    if (!carved.Contains(candidate) && IsInsideMazeBounds(candidate))
                        walls.Add(candidate);
                }
            }
        }

        return walls;
    }

    /// <summary>
    /// Places or maintains a wall entity at the given tile coordinate.
    /// Skips placement if a tile already exists or a protected entity occupies the space.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="tile">The tile coordinate to place the wall at.</param>
    /// <param name="floorDef">Floor tile definition (used for reference).</param>
    private void EnsureWallAtTile(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile, ContentTileDefinition floorDef)
    {

        if (_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef)
            && !tileRef.Tile.IsEmpty
            && !_turf.IsSpace(tileRef))
            return;

        if (ContainsProtectedAnchoredEntity(gridUid, gridComp, tile))
            return;

        _map.SetTile(gridUid, gridComp, tile, Tile.Empty);

        var coords = _map.GridTileToLocal(gridUid, gridComp, tile);
        if (coords.IsValid(EntityManager))
            Spawn("WallInvisiblePermanent", coords);
    }

    /// <summary>
    /// Tests whether a tile contains any protected death-maze marker entities.
    /// </summary>
    /// <param name="gridUid">The grid entity.</param>
    /// <param name="gridComp">The grid component.</param>
    /// <param name="tile">The tile to check.</param>
    /// <returns>True if the tile contains a protected entity.</returns>
    private bool ContainsProtectedAnchoredEntity(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile)
    {
        var anchored = _map.GetAnchoredEntities(gridUid, gridComp, tile);
        foreach (var uid in anchored)
        {
            if (IsProtectedEntity(uid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Carves a single hallway tile, removing non-protected entities and updating the floor.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="tile">The tile to carve.</param>
    /// <param name="floorDef">Floor tile definition to apply.</param>
    private void CarveHallTile(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile, ContentTileDefinition floorDef)
    {
        var anchored = _map.GetAnchoredEntities(gridUid, gridComp, tile);
        foreach (var uid in anchored)
        {
            if (!IsProtectedEntity(uid))
                QueueDel(uid);
        }

        if (_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef)
            && !tileRef.Tile.IsEmpty
            && !_turf.IsSpace(tileRef))
        {
            return;
        }

        _map.SetTile(gridUid, gridComp, tile, new Tile(floorDef.TileId));
    }

    /// <summary>
    /// Ensures the maze origin tile (0,0) is accessible and carved as a hallway.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="floorDef">Floor tile definition to apply.</param>
    private void EnsureAccessibleOrigin(EntityUid gridUid, MapGridComponent gridComp, ContentTileDefinition floorDef)
    {
        CarveHallTile(gridUid, gridComp, new Vector2i(0, 0), floorDef);
    }

    /// <summary>
    /// Attempts to create a new room adjacent to an existing room via a corridor.
    /// Handles corridor branching with optional perpendicular turns.
    /// </summary>
    /// <param name="carved">Set tracking all carved tiles (rooms and hallways).</param>
    /// <param name="hallwayTiles">Set tracking hallway-only tiles.</param>
    /// <param name="rooms">List of placed rooms for procedural generation.</param>
    /// <param name="perimeterHalfSize">Maximum distance from center for valid room placement.</param>
    /// <param name="minCorridorLength">Minimum corridor length between rooms.</param>
    /// <param name="maxCorridorLength">Maximum corridor length between rooms.</param>
    /// <returns>True if a new room was successfully created and added.</returns>
    private bool TryAddRoom(
        HashSet<Vector2i> carved,
        HashSet<Vector2i> hallwayTiles,
        List<MazeRoomFootprint> rooms,
        int perimeterHalfSize,
        int minCorridorLength,
        int maxCorridorLength)
    {
        if (rooms.Count == 0)
            return false;

        var sourceRoom = rooms[_random.Next(rooms.Count)];
        var direction = CardinalDirections[_random.Next(CardinalDirections.Length)];
        var sourceDoor = GetRandomRoomDoorTile(sourceRoom, direction);
        var corridorTiles = new List<Vector2i>();

        var firstLegLength = _random.Next(minCorridorLength, maxCorridorLength + 1);
        if (!TryAppendCorridorLeg(corridorTiles, sourceDoor, direction, firstLegLength, perimeterHalfSize, rooms, out var corridorEnd))
            return false;

        var finalDirection = direction;
        if (_random.Next(100) < 65)
        {
            var turnDirection = GetRandomPerpendicularDirection(direction);
            var secondLegLength = _random.Next(minCorridorLength, maxCorridorLength + 1);
            if (TryAppendCorridorLeg(corridorTiles, corridorEnd, turnDirection, secondLegLength, perimeterHalfSize, rooms, out var turnedEnd))
            {
                corridorEnd = turnedEnd;
                finalDirection = turnDirection;
            }
        }

        var doorway = corridorEnd + finalDirection;
        if (!TryCreateRoomFromDoorway(doorway, finalDirection, PickRoomSize(), PickRoomSize(), out var room))
            return false;

        if (!IsRoomInsidePerimeterBounds(room, perimeterHalfSize) || !IsRoomPlacementClear(room, rooms, carved))
            return false;

        foreach (var corridorTile in corridorTiles)
        {
            carved.Add(corridorTile);
            hallwayTiles.Add(corridorTile);
        }

        CarveRoom(carved, room);
        rooms.Add(room);
        return true;
    }
    /// <summary>
    /// Attempts to extend a corridor in a specified direction for a given length.
    /// Validates collision with existing rooms and perimeter bounds.
    /// </summary>
    /// <param name="corridor">List of corridor tiles being built.</param>
    /// <param name="start">The starting tile for this corridor leg.</param>
    /// <param name="direction">The cardinal direction to extend.</param>
    /// <param name="length">Number of tiles to carve in this direction.</param>
    /// <param name="perimeterHalfSize">Maximum distance from center for valid placement.</param>
    /// <param name="rooms">List of existing rooms for collision detection.</param>
    /// <param name="end">The final tile coordinate of the extended corridor when successful.</param>
    /// <returns>True if the corridor was successfully extended without collision.</returns>
    private bool TryAppendCorridorLeg(
        List<Vector2i> corridorTiles,
        Vector2i start,
        Vector2i direction,
        int length,
        int perimeterHalfSize,
        List<MazeRoomFootprint> rooms,
        out Vector2i end)
    {
        var current = start;

        for (var i = 0; i < length; i++)
        {
            var next = current + direction;
            if (!IsInsidePerimeterBounds(next, perimeterHalfSize) || IsTileInsideAnyRoom(next, rooms))
            {
                end = start;
                return false;
            }

            current = next;
            corridorTiles.Add(current);
        }

        end = current;
        return corridorTiles.Count > 0;
    }

    /// <summary>
    /// Carves all tiles of a room into the carved set.
    /// </summary>
    /// <param name="carved">The set to add room tiles to.</param>
    /// <param name="room">The room footprint to carve.</param>
    private void CarveRoom(HashSet<Vector2i> carved, MazeRoomFootprint room)
    {
        for (var x = room.Min.X; x <= room.Max.X; x++)
        {
            for (var y = room.Min.Y; y <= room.Max.Y; y++)
                carved.Add(new Vector2i(x, y));
        }
    }

    /// <summary>
    /// Spawns room fill markers at the center of maximum-sized rooms for entity distribution.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="rooms">List of rooms to process for marker spawning.</param>
    private void SpawnRoomFillMarkers(EntityUid gridUid, MapGridComponent gridComp, List<MazeRoomFootprint> rooms)
    {
        foreach (var room in rooms)
        {
            if (room.Width != DeathMazeMaxRoomSize || room.Height != DeathMazeMaxRoomSize)
                continue;

            var coords = _map.GridTileToLocal(gridUid, gridComp, room.Center);
            if (coords.IsValid(EntityManager))
                Spawn(DeathMazeRoomFillMarkerPrototype, coords);
        }
    }

    /// <summary>
    /// Builds sampled hallway tiles via radial line casting from the maze center.
    /// Used for fallback spawn placement when no explicit spawn markers exist.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="rule">The active Slasher rule component for sampling configuration.</param>
    /// <returns>Set of hallway tiles identified via line casting.</returns>
    private HashSet<Vector2i> BuildSampledHallwayFallbackTiles(
        EntityUid gridUid,
        MapGridComponent gridComp,
        HashSet<Vector2i> reachableTiles,
        Vector2i centerTile,
        SlasherRuleComponent rule)
    {
        var result = new HashSet<Vector2i>();
        var lineCount = Math.Max(0, rule.DeathMazeSampleLineCount);
        var lineLength = Math.Max(1, rule.DeathMazeSampleLineLength);

        if (lineCount == 0 || reachableTiles.Count == 0)
            return result;

        for (var i = 0; i < lineCount; i++)
        {
            var direction = _random.Pick(CardinalDirections);
            var current = centerTile;

            for (var step = 0; step < lineLength; step++)
            {
                current += direction;

                if (!reachableTiles.Contains(current) || !IsValidTile(gridUid, gridComp, current))
                    continue;

                if (!HasHallwayWallsOnBothSides(gridUid, gridComp, current, direction))
                    continue;

                result.Add(current);
            }
        }

        return result;
    }

    /// <summary>
    /// Tests whether a hallway tile has blocking boundaries or walls on both perpendicular sides.
    /// Used to identify open hallway corridors suitable for spawning.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="tile">The tile to test.</param>
    /// <param name="direction">The direction of the hallway (to determine perpendicular sides).</param>
    /// <returns>True if both sides have blocking boundaries or empty space.</returns>
    private bool HasHallwayWallsOnBothSides(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile, Vector2i direction)
    {
        var leftOffset = new Vector2i(-direction.Y, direction.X);
        var rightOffset = new Vector2i(direction.Y, -direction.X);

        return IsBlockingBoundaryOrWallTile(gridUid, gridComp, tile + leftOffset)
            && IsBlockingBoundaryOrWallTile(gridUid, gridComp, tile + rightOffset);
    }

    /// <summary>
    /// Tests whether a tile is blocked by a boundary or is empty/space (impassable).
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="tile">The tile to test.</param>
    /// <returns>True if the tile is blocking or out of bounds.</returns>
    private bool IsBlockingBoundaryOrWallTile(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef))
            return true;

        return tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef);
    }

    /// <summary>
    /// Picks a random room size within configured bounds.
    /// </summary>
    /// <returns>A random valid room size for maze generation.</returns>
    private int PickRoomSize() => DeathMazeMaxRoomSize;

    /// <summary>
    /// Calculates the perimeter half-size for room placement bounds.
    /// </summary>
    /// <param name="lineLength">The configured line length for sampling.</param>
    /// <returns>The half-size perimeter bound for room placement validation.</returns>
    private int GetPerimeterHalfSize(int lineLength)
        => Math.Clamp((lineLength * 3) / 4, 20, DeathMazeCanvasHalfSize - 8);

    /// <summary>
    /// Creates a rectangular room footprint centered at the given point with specified dimensions.
    /// </summary>
    /// <param name="center">The center tile coordinate for the room.</param>
    /// <param name="width">The room width in tiles.</param>
    /// <param name="height">The room height in tiles.</param>
    /// <returns>A room footprint with min/max bounds.</returns>
    private MazeRoomFootprint CreateCenteredRoom(Vector2i center, int width, int height)
    {
        var min = new Vector2i(center.X - (width / 2), center.Y - (height / 2));
        var max = new Vector2i(min.X + width - 1, min.Y + height - 1);
        return new MazeRoomFootprint(min, max);
    }

    /// <summary>
    /// Picks a random door tile on the edge of a room facing a given direction.
    /// </summary>
    /// <param name="room">The room footprint.</param>
    /// <param name="direction">The direction the door should face.</param>
    /// <returns>The door tile coordinate.</returns>
    private Vector2i GetRandomRoomDoorTile(MazeRoomFootprint room, Vector2i direction)
    {
        var minX = room.Min.X + (room.Width > 2 ? 1 : 0);
        var maxX = room.Max.X - (room.Width > 2 ? 1 : 0);
        var minY = room.Min.Y + (room.Height > 2 ? 1 : 0);
        var maxY = room.Max.Y - (room.Height > 2 ? 1 : 0);

        if (direction.X > 0) return new Vector2i(room.Max.X, _random.Next(minY, maxY + 1));
        if (direction.X < 0) return new Vector2i(room.Min.X, _random.Next(minY, maxY + 1));
        if (direction.Y > 0) return new Vector2i(_random.Next(minX, maxX + 1), room.Max.Y);
        return new Vector2i(_random.Next(minX, maxX + 1), room.Min.Y);
    }

    /// <summary>
    /// Attempts to create a new room positioned from a doorway in a given direction.
    /// </summary>
    /// <param name="doorway">The doorway tile opening into the new room.</param>
    /// <param name="incomingDirection">The direction the corridor was traveling.</param>
    /// <param name="width">The desired room width.</param>
    /// <param name="height">The desired room height.</param>
    /// <param name="room">The created room footprint when successful.</param>
    /// <returns>True if the room was successfully created.</returns>
    private bool TryCreateRoomFromDoorway(Vector2i doorway, Vector2i incomingDirection, int width, int height, out MazeRoomFootprint room)
    {
        room = default;
        if (width < DeathMazeMinRoomSize || height < DeathMazeMinRoomSize)
            return false;

        if (incomingDirection.X > 0)
        {
            var minY = doorway.Y - _random.Next(height);
            room = new MazeRoomFootprint(new Vector2i(doorway.X, minY), new Vector2i(doorway.X + width - 1, minY + height - 1));
            return true;
        }

        if (incomingDirection.X < 0)
        {
            var minY = doorway.Y - _random.Next(height);
            room = new MazeRoomFootprint(new Vector2i(doorway.X - width + 1, minY), new Vector2i(doorway.X, minY + height - 1));
            return true;
        }

        if (incomingDirection.Y > 0)
        {
            var minX = doorway.X - _random.Next(width);
            room = new MazeRoomFootprint(new Vector2i(minX, doorway.Y), new Vector2i(minX + width - 1, doorway.Y + height - 1));
            return true;
        }

        if (incomingDirection.Y < 0)
        {
            var minX = doorway.X - _random.Next(width);
            room = new MazeRoomFootprint(new Vector2i(minX, doorway.Y - height + 1), new Vector2i(minX + width - 1, doorway.Y));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tests whether a room placement location is clear of collisions with existing rooms and carved tiles.
    /// </summary>
    /// <param name="room">The room to test for placement.</param>
    /// <param name="rooms">List of existing rooms for collision detection.</param>
    /// <param name="carved">Set of already-carved tiles.</param>
    /// <returns>True if the room can be placed without collision.</returns>
    private bool IsRoomPlacementClear(MazeRoomFootprint room, List<MazeRoomFootprint> rooms, HashSet<Vector2i> carved)
    {
        foreach (var existing in rooms)
        {
            if (RoomsOverlapWithPadding(room, existing, 1))
                return false;
        }

        for (var x = room.Min.X; x <= room.Max.X; x++)
        {
            for (var y = room.Min.Y; y <= room.Max.Y; y++)
            {
                if (carved.Contains(new Vector2i(x, y)))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Tests whether a tile is contained within any room in the given list.
    /// </summary>
    /// <param name="tile">The tile to test.</param>
    /// <param name="rooms">List of rooms to check.</param>
    /// <returns>True if the tile is inside any room.</returns>
    private bool IsTileInsideAnyRoom(Vector2i tile, List<MazeRoomFootprint> rooms)
    {
        foreach (var room in rooms)
        {
            if (tile.X >= room.Min.X && tile.X <= room.Max.X
                && tile.Y >= room.Min.Y && tile.Y <= room.Max.Y)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tests whether two rooms overlap when accounting for padding distance.
    /// </summary>
    /// <param name="first">The first room footprint.</param>
    /// <param name="second">The second room footprint.</param>
    /// <param name="padding">Minimum distance to maintain between rooms.</param>
    /// <returns>True if the rooms overlap with padding considered.</returns>
    private bool RoomsOverlapWithPadding(MazeRoomFootprint first, MazeRoomFootprint second, int padding)
    {
        return first.Min.X - padding <= second.Max.X
               && first.Max.X + padding >= second.Min.X
               && first.Min.Y - padding <= second.Max.Y
               && first.Max.Y + padding >= second.Min.Y;
    }

    /// <summary>
    /// Tests whether a room footprint is entirely within the perimeter bounds.
    /// </summary>
    /// <param name="room">The room footprint to test.</param>
    /// <param name="perimeterHalfSize">Maximum distance from center for valid placement.</param>
    /// <returns>True if all room tiles are within perimeter bounds.</returns>
    private bool IsRoomInsidePerimeterBounds(MazeRoomFootprint room, int perimeterHalfSize)
        => IsInsidePerimeterBounds(room.Min, perimeterHalfSize) && IsInsidePerimeterBounds(room.Max, perimeterHalfSize);

    /// <summary>
    /// Tests whether a tile coordinate is within the perimeter bounds.
    /// </summary>
    /// <param name="tile">The tile to test.</param>
    /// <param name="perimeterHalfSize">Maximum distance from center for valid placement.</param>
    /// <returns>True if the tile is within perimeter bounds.</returns>
    private static bool IsInsidePerimeterBounds(Vector2i tile, int perimeterHalfSize)
        => Math.Abs(tile.X) <= perimeterHalfSize && Math.Abs(tile.Y) <= perimeterHalfSize;

    /// <summary>
    /// Tests whether a tile coordinate is within the maze canvas bounds, with optional padding.
    /// </summary>
    /// <param name="tile">The tile to test.</param>
    /// <param name="padding">Optional distance to pad from the canvas edge.</param>
    /// <returns>True if the tile is within the maze canvas bounds.</returns>
    private static bool IsInsideMazeBounds(Vector2i tile, int padding = 0)
    {
        var max = DeathMazeCanvasHalfSize - padding;
        return Math.Abs(tile.X) <= max && Math.Abs(tile.Y) <= max;
    }

    /// <summary>
    /// Picks a random perpendicular direction (90 degrees) relative to the given direction.
    /// </summary>
    /// <param name="direction">The current direction vector.</param>
    /// <returns>A perpendicular cardinal direction.</returns>
    private Vector2i GetRandomPerpendicularDirection(Vector2i direction)
    {
        if (direction.X != 0)
            return _random.Next(2) == 0 ? new Vector2i(0, 1) : new Vector2i(0, -1);

        return _random.Next(2) == 0 ? new Vector2i(1, 0) : new Vector2i(-1, 0);
    }
}