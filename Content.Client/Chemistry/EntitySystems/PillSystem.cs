using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class PillSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PillComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, PillComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!component.SpriteUsesType)
        {
            return;
        }

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!_sprite.TryGetLayer((uid, sprite), 0, out var layer, false))
            return;

        _sprite.LayerSetRsiState(layer, $"pill{component.PillType + 1}");
    }
}
