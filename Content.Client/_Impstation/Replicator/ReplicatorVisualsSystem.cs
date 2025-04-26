// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared._Impstation.Replicator;
using Content.Shared.CombatMode;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Replicator;

public sealed class ReplicatorVisualsSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ReplicatorComponent, ToggleCombatActionEvent>(OnToggleCombat);

        SubscribeLocalEvent<ReplicatorNestComponent, AppearanceChangeEvent>(OnNestAppearanceChange);
        SubscribeLocalEvent<ReplicatorNestComponent, ReplicatorNestSizeChangedEvent>(OnNestSizeChanged);
    }

    private void OnToggleCombat(Entity<ReplicatorComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _appearance.OnChangeData(ent, sprite);
    }

    private void OnAppearanceChange(Entity<ReplicatorComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!TryComp<CombatModeComponent>(ent, out var combat))
            return;
        if (!args.Sprite.LayerMapTryGet(ReplicatorVisuals.Combat, out var layer))
            return;
        args.Sprite.LayerSetVisible(layer, combat.IsInCombatMode);
    }

    private void OnNestSizeChanged(Entity<ReplicatorNestComponent> ent, ref ReplicatorNestSizeChangedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _appearance.OnChangeData(ent, sprite);
    }

    private void OnNestAppearanceChange(Entity<ReplicatorNestComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        int layer;
        if (ent.Comp.CurrentLevel == 2 && args.Sprite.LayerMapTryGet(ReplicatorNestVisuals.Level2, out layer))
            args.Sprite.LayerSetVisible(layer, true);
        else if (ent.Comp.CurrentLevel == 3 && args.Sprite.LayerMapTryGet(ReplicatorNestVisuals.Level3, out layer))
            args.Sprite.LayerSetVisible(layer, true);
    }
}
