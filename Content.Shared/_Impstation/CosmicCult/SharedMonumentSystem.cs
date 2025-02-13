using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.CosmicCult;
public sealed class SharedMonumentSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
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
        // TODO: Add what you want to do here!

        // TODO: REMOVE ME! THIS IS FOR DEMO PURPOSES ONLY!! (Although you probably want to do something similar or exactly this)
        ent.Comp.SelectedGlyph = args.GlyphProtoId;


        _uiSystem.SetUiState(ent.Owner, MonumentKey.Key, GenerateBuiState(ent.Comp));
    }

    private void OnInfluenceSelected(Entity<MonumentComponent> ent, ref InfluenceSelectedMessage args)
    {
        // TODO: Add what you want to do here!

        // TODO: REMOVE ME! THIS IS FOR DEMO PURPOSES ONLY!!
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
    #endregion
}
