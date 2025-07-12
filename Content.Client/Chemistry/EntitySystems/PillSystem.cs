using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Random; // imp edit

namespace Content.Client.Chemistry.EntitySystems;

public sealed class PillSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // imp edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PillComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<PillComponent, ComponentInit>(OnComponentInit); // imp edit
    }

    private void OnHandleState(EntityUid uid, PillComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!_sprite.TryGetLayer((uid, sprite), 0, out var layer, false))
            return;

        // imp edit start
        if (!component.RandomType)
        {
            _sprite.LayerSetRsiState(layer, $"pill{component.PillType + 1}");
        }
        // imp edit end
    }

    // imp edit start
    private void OnComponentInit(EntityUid uid, PillComponent component, ref ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!_sprite.TryGetLayer((uid, sprite), 0, out var layer, false))
            return;

        if (component.RandomType)
        {
            _sprite.LayerSetRsiState(layer, $"pill{_random.Next(20) + 1}");
        }
    }
    // imp edit end
}
