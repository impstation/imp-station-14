using System.Numerics;
using Content.Server.Spreader;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared._Impstation.CrystalMass;
using Content.Shared.Damage.Components;
using Content.Shared.Ghost;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Spreader;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> CrystalMassGroup = "CrystalMass";
    private static readonly ProtoId<ContentTileDefinition> CrystalMassPlating = "PlatingCrystalMass";


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(SetupCrystalMass);
        SubscribeLocalEvent<CrystalMassComponent, SpreadNeighborsEvent>(OnCrystalSpread);

        SubscribeLocalEvent<CrystalMassComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrystalMassComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Requires a delay so that entities can register being on the tile for ClearTile
            if (!comp.ClearedTile && comp.ClearTime < _timing.CurTime)
            {
                ClearTile((uid, comp));

                // Delay adding pointlight for when multiple are on one tile deleting each other so that it isn't jarring
                if (comp.IsBulb)
                {
                    EnsureComp<PointLightComponent>(uid);
                    _lights.SetRadius(uid, comp.BulbRadius);
                    _lights.SetEnergy(uid, comp.BulbEnergy);
                    _lights.SetColor(uid, comp.BulbColor);
                }
                comp.ClearedTile = true;
            }
        }
    }

    private void SetupCrystalMass(Entity<CrystalMassComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ClearTime = _timing.CurTime + ent.Comp.ClearTileDelay;

        if (!ent.Comp.StartupAppearance)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        _appearance.SetData(ent, CrystalMassVisuals.Variant, _robustRandom.Next(1, ent.Comp.SpriteVariants + 1), appearance);
    }

    private void OnCrystalSpread(Entity<CrystalMassComponent> ent, ref SpreadNeighborsEvent args)
    {
        var comp = ent.Comp;

        // Only occurs when surrounded by CrystalMass spreaders
        if (args.Neighbors.Count == 4)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        if (!_robustRandom.Prob(comp.SpreadChance))
            return;

        var prototype = _robustRandom.Prob(comp.BulbChance) ? comp.CrystalBulbPrototype : comp.CrystalMassPrototype;
        var neighbor = _robustRandom.Pick(args.AllNeighbors);
        var neighborCoords = _map.GridTileToLocal(neighbor.GridUid, neighbor.Grid, neighbor.Position);

        HandleTiles(neighbor.GridUid, neighbor.Grid, neighbor.Position);

        var neighborUid = Spawn(prototype, neighborCoords);
        DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == CrystalMassGroup);

        args.Updates--;
        if (args.Updates <= 0)
            return;
    }

    private void HandleTiles(EntityUid neighborGridUid, MapGridComponent neighborGrid, Vector2i neighborPosition)
    {
        var mapID = Transform(neighborGridUid).MapID;
        var worldPos = _map.GridTileToWorldPos(neighborGridUid, neighborGrid, neighborPosition);
        var box = Box2.CenteredAround(worldPos, Vector2.One);
        var circle = new Circle(worldPos, 0.5f);

        var grids = new List<Entity<MapGridComponent>>();
        _mapManager.FindGridsIntersecting(mapID, box, ref grids);

        // Locating every intersecting grid in the neighbor CrystalMass is about to spread to
        foreach (var grid in grids)
        {
            if (grid.Owner == neighborGridUid)
                continue;

            // Locating every tile within those intsersecting grids that are within radius
            foreach (var tile in _map.GetTilesIntersecting(grid.Owner, grid.Comp, circle))
                _map.SetTile(grid.Owner, grid, tile.GridIndices, Tile.Empty);
        }

        var seed = _robustRandom.Next();
        var random = new Random(seed);
        var variant = _tile.PickVariant((ContentTileDefinition)_tileDefManager[CrystalMassPlating], random);

        _map.SetTile(neighborGridUid, neighborGrid, neighborPosition, new Tile(_tileDefManager[CrystalMassPlating].TileId, 0, variant));
    }

    private void ClearTile(Entity<CrystalMassComponent> ent)
    {
        var xform = Transform(ent);
        var gridUid = xform.GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tilePos = _map.LocalToTile(gridUid.Value, grid, xform.Coordinates);

        foreach (var target in _lookup.GetLocalEntitiesIntersecting(gridUid.Value, tilePos, flags: LookupFlags.Uncontained))
        {
            if (target == ent.Owner)
                continue;

            if (HasComp<SupermatterImmuneComponent>(target)
                || HasComp<GodmodeComponent>(target)
                || HasComp<GhostComponent>(target))
                continue;

            // Prevent multiple crystal mass entities on one tile from queuing everyones downfall
            if (HasComp<CrystalMassComponent>(target))
                RemComp<CrystalMassComponent>(target);

            // Text for players could be added eventually
            if (HasComp<MobStateComponent>(target)
                || HasComp<ItemComponent>(target))
            {
                var targetXform = Transform(target);
                _audio.PlayStatic(ent.Comp.DustSound, Filter.Pvs(target), targetXform.Coordinates, true, AudioParams.Default.WithVolume(-2f));
            }

            EntityManager.QueueDeleteEntity(target);
        }
    }

    private void OnStepTriggered(Entity<CrystalMassComponent> ent, ref StepTriggeredOnEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper)
            || HasComp<ItemComponent>(args.Tripper))
        {
            var xform = Transform(args.Tripper);
            _audio.PlayStatic(ent.Comp.DustSound, Filter.Pvs(args.Tripper), xform.Coordinates, true, AudioParams.Default.WithVolume(-2f));
        }

        EntityManager.QueueDeleteEntity(args.Tripper);
    }

    private void OnStepTriggerAttempt(Entity<CrystalMassComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<SupermatterImmuneComponent>(args.Tripper)
            || HasComp<GodmodeComponent>(args.Tripper)
            || HasComp<GhostComponent>(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }
}
