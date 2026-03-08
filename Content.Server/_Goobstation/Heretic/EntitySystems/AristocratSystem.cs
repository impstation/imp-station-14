using Content.Server.Atmos.EntitySystems;
using Content.Server.Heretic.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Heretic;
using Content.Shared.Maps;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Content.Server._Goobstation.Heretic.Components;
using Content.Shared.Temperature.Components;

namespace Content.Server.Heretic.EntitySystems;

// void path heretic exclusive
public sealed partial class AristocratSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AristocratComponent>();
        while (query.MoveNext(out var uid, out var aristocrat))
        {
            aristocrat.UpdateTimer += frameTime;

            if (aristocrat.UpdateTimer >= aristocrat.UpdateDelay)
            {
                Cycle((uid, aristocrat));
                aristocrat.UpdateTimer = 0;
            }
        }
    }

    private void Cycle(Entity<AristocratComponent> ent)
    {
        SpawnTiles(ent);

        var mix = _atmos.GetTileMixture((ent, Transform(ent)));
        if (mix != null)
            mix.Temperature -= 100f;

        // replace certain things with their winter analogue
        var lookup = _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.Range);
        foreach (var look in lookup)
        {
            if (HasComp<HereticComponent>(look) || HasComp<MinionComponent>(look))
                continue;

            if (TryComp<TemperatureComponent>(look, out var temp))
                _temp.ChangeHeat(look, -200f, true, temp);

            if (!TryComp<TagComponent>(look, out var tag))
                continue;

            var tags = tag.Tags;

            // check if wall
            if (!_rand.Prob(.45f) || !tags.Contains("Wall") || Prototype(look) == null
                || Prototype(look)!.ID == ent.Comp.SnowWallPrototype)
                continue;
            // replace wall
            Spawn(ent.Comp.SnowWallPrototype, Transform(look).Coordinates);
            QueueDel(look);
        }
    }


    private void SpawnTiles(Entity<AristocratComponent> ent)
    {
        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var worldPos = _transform.GetWorldPosition(ent);
        var pos = xform.Coordinates.Position;
        var tilerefs = _map.GetTilesIntersecting(
                xform.GridUid.Value,
                grid,
                new Box2(worldPos + new Vector2(-ent.Comp.Range), worldPos + new Vector2(ent.Comp.Range)))
            .ToList();

        if (tilerefs.Count == 0)
            return;

        var tiles = new List<TileRef>();
        foreach (var tile in tilerefs)
        {
            if (_rand.Prob(.45f))
                tiles.Add(tile);
        }

        foreach (var tileref in tiles)
        {
            var tile = _prot.Index<ContentTileDefinition>(ent.Comp.IceTilePrototype);
            _tile.ReplaceTile(tileref, tile);
        }
    }
}
