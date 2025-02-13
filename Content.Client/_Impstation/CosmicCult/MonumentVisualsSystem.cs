using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;

namespace Content.Client._Impstation.CosmicCult;

/// <summary>
/// Visualizer for The Monument of the Cosmic Cult.
/// </summary>
public sealed class MonumentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, MonumentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!args.Sprite.LayerMapTryGet(MonumentVisualLayers.TransformLayer, out var transformLayer))
            return;
        if (!args.Sprite.LayerMapTryGet(MonumentVisualLayers.MonumentLayer, out var baseLayer))
            return;
        if (!_appearance.TryGetData<bool>(uid, MonumentVisuals.Transforming, out var transforming, args.Component))
            return;
        if (!_appearance.TryGetData<bool>(uid, MonumentVisuals.Tier3, out var tier3, args.Component))
            return;
        if (!tier3)
            args.Sprite.LayerSetState(transformLayer, "transform-stage2");
        if (tier3)
            args.Sprite.LayerSetState(transformLayer, "transform-stage3");
        if (transforming && HasComp<MonumentTransformingComponent>(uid))
        {
            args.Sprite.LayerSetAnimationTime(transformLayer, 0f);
            args.Sprite.LayerSetVisible(transformLayer, true);
            args.Sprite.LayerSetVisible(baseLayer, false);
        }
        else
        {
            args.Sprite.LayerSetVisible(transformLayer, false);
            args.Sprite.LayerSetVisible(baseLayer, true);
        }
    }
}
