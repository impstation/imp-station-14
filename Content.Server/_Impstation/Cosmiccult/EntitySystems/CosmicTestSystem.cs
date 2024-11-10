using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Server.Destructible;
using Content.Server.Temperature.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.MagicMirror;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server._Impstation.Cosmiccult.EntitySystems;
public sealed partial class CosmicTestSystem : EntitySystem
{
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TemperatureSystem _temp = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var cosmicfella in EntityQuery<CosmicTestComponent>())
        {
            cosmicfella.UpdateTimer += frameTime;

            if (cosmicfella.UpdateTimer >= cosmicfella.UpdateDelay)
            {
                Cycle((cosmicfella.Owner, cosmicfella));
                cosmicfella.UpdateTimer = 0;
            }
        }
    }

    private void Cycle(Entity<CosmicTestComponent> ent)
    {
        DeleteTiles(ent);

        var lookup = _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.Range);
        foreach (var look in lookup)
        {
            if ((HasComp<ConstructionComponent>(look) || HasComp<DestructibleComponent>(look) || HasComp<PullableComponent>(look) || HasComp<StepTriggerComponent>(look)
            || HasComp<MagicMirrorComponent>(look) || HasComp<FloorOccluderComponent>(look) || HasComp<DamageableComponent>(look)) && !(HasComp<MindContainerComponent>(look) || HasComp<BodyComponent>(look)))
            {
                QueueDel(look);
            }
        }
    }

    private void DeleteTiles(Entity<CosmicTestComponent> ent)
    {
        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = xform.Coordinates.Position;
        var box = new Box2(pos + new Vector2(-ent.Comp.Range, -ent.Comp.Range), pos + new Vector2(ent.Comp.Range, ent.Comp.Range));
        var tilerefs = grid.GetLocalTilesIntersecting(box).ToList();

        if (tilerefs.Count == 0)
            return;

        var tiles = new List<TileRef>();
        foreach (var tile in tilerefs)
        {
            if (_rand.Prob(.40f))
                tiles.Add(tile);
        }

        foreach (var tileref in tiles)
        {
            var tile = _prot.Index<ContentTileDefinition>("FloorGlass");
            _tile.ReplaceTile(tileref, tile);
        }
    }
}
