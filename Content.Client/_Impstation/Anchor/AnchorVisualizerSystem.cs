using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Anchor;

public sealed partial class AnchorVisualizerSystem : VisualizerSystem<AnchorVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorVisualsComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    protected override void OnAppearanceChange(EntityUid uid, AnchorVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, comp, ref args);

        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, AnchorVisuals.Anchored, out var anchored, args.Component))
            return;

        if (!args.Sprite.LayerMapTryGet(AnchorVisualLayers.Base, out var layer, true))
            return;

        if (anchored)
            args.Sprite.LayerSetVisible(layer, comp.StateAnchored != null);
        else
            args.Sprite.LayerSetVisible(layer, comp.StateUnanchored != null);

        args.Sprite.LayerSetState(layer, anchored ? comp.StateAnchored : comp.StateUnanchored);
    }

    private void OnAnchorStateChanged(EntityUid uid, AnchorVisualsComponent comp, AnchorStateChangedEvent args)
    {
        AppearanceSystem.SetData(uid, AnchorVisuals.Anchored, args.Anchored);
    }
}

public enum AnchorVisuals : byte
{
    Anchored
}

public enum AnchorVisualLayers : byte
{
    Base
}
