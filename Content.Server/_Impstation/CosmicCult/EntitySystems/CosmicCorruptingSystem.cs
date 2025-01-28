using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Tag;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;
public sealed partial class CosmicCorruptingSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    private string _cosmicWallProto = "WallCosmicCult";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blanktimer = EntityQueryEnumerator<CosmicCorruptingComponent>();
        while (blanktimer.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.CorruptionValue)
            {
                comp.CorruptionValue = _timing.CurTime + comp.CorruptionSpeed;
                ConvertTilesInRange((uid, comp));
            }
        }
    }

    private void ConvertTilesInRange(Entity<CosmicCorruptingComponent> uid)
    {
        var tgtPos = Transform(uid);
        if (tgtPos.GridUid is not { } gridUid || !TryComp(tgtPos.GridUid, out MapGridComponent? mapGrid))
            return;

        var radius = uid.Comp.CorruptionRadius;

        var tileList = _map.GetLocalTilesEnumerator(gridUid, mapGrid, new Box2(tgtPos.Coordinates.Position + new Vector2(-radius, -radius), tgtPos.Coordinates.Position + new Vector2(radius, radius)));
        var entityList = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, radius);
        foreach (var entity in entityList)
        {
            if (TryComp<TagComponent>(entity, out var tag))
            {
                var tags = tag.Tags;
                if (tags.Contains("Wall") && Prototype(entity) != null && Prototype(entity)!.ID != _cosmicWallProto)
                {
                    Spawn(_cosmicWallProto, Transform(entity).Coordinates);
                    QueueDel(entity);
                }
            }
        }
        while (tileList.MoveNext(out var tile))
        {
            var tilePos = _turfs.GetTileCenter(tile);
            var cultTileDefinition = (ContentTileDefinition)_tileDefinition[_random.Pick(uid.Comp.CultTile)];
            if (tile.Tile.TypeId == cultTileDefinition.TileId)
                continue;
            if (tile.GetContentTileDefinition().Name != "tiles-cosmiccult-floor-notched")
            {
                _tile.ReplaceTile(tile, cultTileDefinition);
                Spawn(uid.Comp.TileConvertVFX, tilePos);
            }
        }
    }
}
