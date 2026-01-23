using System.Numerics;
using Content.Shared._Impstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.Administration.Systems;

public sealed class SwordDamoclesSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(OnAdded);
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentShutdown>(OnRemoved);
    }

    private void OnRemoved(Entity<SwordDamoclesComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), SwordDamoclesKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private void OnAdded(Entity<SwordDamoclesComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent, sprite), SwordDamoclesKey.Key, out var _, false))
            return;

        var adj = _sprite.GetLocalBounds((ent, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((ent, sprite), new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/KillSign.rsi"), "sign")); // change to Impstation/Objects/Misc/SwordDamocles when that sprite exists
        _sprite.LayerMapSet((ent, sprite), SwordDamoclesKey.Key, layer);

        _sprite.LayerSetOffset((ent, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum SwordDamoclesKey
    {
        Key,
    }
}
