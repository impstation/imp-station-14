using System.Linq;
using System.Numerics;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.CosmicCult;
public sealed class SharedMonumentSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem  _actions = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonumentComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<MonumentComponent, GlyphSelectedMessage>(OnGlyphSelected);
        SubscribeLocalEvent<MonumentComponent, InfluenceSelectedMessage>(OnInfluenceSelected);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MonumentTransformingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;
            _appearance.SetData(uid, MonumentVisuals.Transforming, false);
            RemComp<MonumentTransformingComponent>(uid);
        }
    }

    private void OnUIOpened(Entity<MonumentComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, MonumentKey.Key))
            return;
        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }

    #region UI listeners
    private void OnGlyphSelected(Entity<MonumentComponent> ent, ref GlyphSelectedMessage args)
    {
        // TODO: this needs checks for tier, or mote cost, or whatever you want to do here

        ent.Comp.SelectedGlyph = args.GlyphProtoId; // not sure SelectedGlyph is needed for anything? keeping it here in case

        if (!_prototype.TryIndex(args.GlyphProtoId, out var proto))
            return;

        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, -1);

        if (_map.GetAnchoredEntities(xform.GridUid.Value, grid, targetIndices).Any(tileEnt => HasComp<CosmicGlyphComponent>(tileEnt)))
            return;

        Spawn(proto.Entity, _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid));

        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }
    private void OnInfluenceSelected(Entity<MonumentComponent> ent, ref InfluenceSelectedMessage args)
    {
        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto))
            return;

        ent.Comp.UnlockedInfluences.Remove(args.InfluenceProtoId);

        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }
    #endregion

    #region Helper functions
    private MonumentBuiState GenerateBuiState(MonumentComponent comp)
    {
        return new MonumentBuiState(
            comp.AvailableEntropy,
            comp.EntropyUntilNextStage,
            comp.CrewToConvertNextStage,
            comp.PercentageComplete,
            comp.SelectedGlyph,
            comp.UnlockedInfluences
        );
    }
    private TileRef? GetGlyphSpawningPoint(Entity<MonumentComponent> ent, MapGridComponent grid)
    {
        var xform = Transform(ent);
        if (xform.GridUid == null)
            return null;
        var localPosition = xform.LocalPosition;
        var tileRefs = _map.GetLocalTilesIntersecting(
                xform.GridUid.Value,
                grid,
                new Box2(localPosition + new Vector2(-1, -1), localPosition + new Vector2(1, -1)))
            .ToList();
        if (tileRefs.Count == 0)
            return null;
        TileRef? result = null;
        while (result == null)
        {
            if (tileRefs.Count == 0)
                break;
            var tileRef = _random.Pick(tileRefs);
            var valid = true;
            foreach (var tileEnt in _map.GetAnchoredEntities(xform.GridUid.Value, grid, tileRef.GridIndices))
            {
                if (!HasComp<CosmicGlyphComponent>(tileEnt))
                    continue;
                valid = false;
                break;
            }
            if (!valid)
            {
                tileRefs.Remove(tileRef);
                continue;
            }
            result = tileRef;
        }
        return result;
    }
    #endregion
}
