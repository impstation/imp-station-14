using Content.Client.DamageState;
using Content.Shared._Impstation.Deadlock;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Deadlock;

public sealed class BurrowVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurrowVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, BurrowVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(BurrowVisuals.VisualState, out var visuals) ||
            visuals is not BurrowVisualState visualState)
        {
            visualState = BurrowVisualState.Normal;
        }

        if (!TryComp<SpriteComponent>(uid, out _))
            return;

        var isBurrowing = !(visualState != BurrowVisualState.Burrowing);
        var isSpinning = visualState == BurrowVisualState.Spinning;

        _sprite.LayerSetVisible(uid, BurrowVisuals.VisualState, isBurrowing);
        _sprite.LayerSetVisible(uid, DamageStateVisualLayers.Base, !isBurrowing);
        _sprite.LayerSetVisible(uid, BurrowVisuals.Spin, isSpinning);
    }
}
