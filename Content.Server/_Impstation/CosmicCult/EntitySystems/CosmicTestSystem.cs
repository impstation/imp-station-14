using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;
public sealed partial class CosmicTestSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sourceQuery = EntityQueryEnumerator<CosmicTestComponent>();
        while (sourceQuery.MoveNext(out var uid, out var source))
        {
            source.CorruptionAccumulator += frameTime;

            if (source.CorruptionAccumulator >= source.CorruptionCooldown)
            {
                source.CorruptionAccumulator = 0;
                ConvertTilesInRange((uid, source));
            }
        }
    }

    private void ConvertTilesInRange(Entity<CosmicTestComponent> source)
    {
        var sourceTrans = Transform(source);
        if (sourceTrans.GridUid is not { } gridUid || !TryComp(sourceTrans.GridUid, out MapGridComponent? mapGrid))
            return;

        var radius = source.Comp.CorruptionRadius;
        var tilesRefs = _map.GetLocalTilesIntersecting(gridUid,
                mapGrid,
                new Box2(sourceTrans.Coordinates.Position + new Vector2(-radius, -radius),
                    sourceTrans.Coordinates.Position + new Vector2(radius, radius)))
            .ToList();

        _random.Shuffle(tilesRefs);

        var cultTileDefinition = (ContentTileDefinition) _tileDefinition[_random.Pick(source.Comp.CultTile)];
        foreach (var tile in tilesRefs)
        {
            if (tile.Tile.TypeId == cultTileDefinition.TileId)
                continue;

            var tilePos = _turfs.GetTileCenter(tile);
            _tile.ReplaceTile(tile, cultTileDefinition);
            Spawn(source.Comp.TileConvertVFX, tilePos);
            return;
        }

    }
}
