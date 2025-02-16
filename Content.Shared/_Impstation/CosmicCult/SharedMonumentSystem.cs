using System.Linq;
using System.Numerics;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonumentComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<MonumentComponent, GlyphSelectedMessage>(OnGlyphSelected);
        SubscribeLocalEvent<MonumentComponent, GlyphRemovedMessage>(OnGlyphRemove);
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
        if (!_uiSystem.IsUiOpen(ent.Owner, MonumentKey.Key) || !TryComp<ActivatableUIComponent>(ent, out var uiComp))
            return;
        if (ent.Comp.Enabled && TryComp<CosmicCultComponent>(uiComp.CurrentSingleUser, out var cultComp))
        {
            ent.Comp.UnlockedInfluences = cultComp.UnlockedInfluences;
            ent.Comp.AvailableEntropy = cultComp.EntropyBudget;
        }
        else _uiSystem.CloseUi(ent.Owner, MonumentKey.Key, uiComp.CurrentSingleUser); // based on the prior IF, this effectively cancels the UI if the user is either not a cultist, or the Finale is ready to trigger.
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

        if (ent.Comp.CurrentGlyph is not null) QueueDel(ent.Comp.CurrentGlyph);
        var glyphEnt = Spawn(proto.Entity, _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid));
        ent.Comp.CurrentGlyph = glyphEnt;

        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }
    private void OnGlyphRemove(Entity<MonumentComponent> ent, ref GlyphRemovedMessage args)
    {
        if (ent.Comp.CurrentGlyph is not null) QueueDel(ent.Comp.CurrentGlyph);
        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }

    private void OnInfluenceSelected(Entity<MonumentComponent> ent, ref InfluenceSelectedMessage args)
    {
        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto) || !TryComp<ActivatableUIComponent>(ent, out var uiComp) || !TryComp<CosmicCultComponent>(uiComp.CurrentSingleUser, out var cultComp))
            return;
        if (ent.Comp.AvailableEntropy < proto.Cost || uiComp.CurrentSingleUser == null)
            return;
        if (proto.InfluenceType == "influence-type-active")
        {
            var actionEnt = _actions.AddAction(uiComp.CurrentSingleUser.Value, proto.Action);
            cultComp.ActionEntities.Add(actionEnt);
        }

        ent.Comp.AvailableEntropy -= proto.Cost;
        cultComp.EntropyBudget -= proto.Cost;
        ent.Comp.UnlockedInfluences.Remove(args.InfluenceProtoId);
        cultComp.UnlockedInfluences.Remove(args.InfluenceProtoId);

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
            comp.UnlockedInfluences,
            comp.UnlockedGlyphs
        );
    }
    #endregion
}
