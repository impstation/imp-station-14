using System.Numerics;
using Content.Client.Administration.Components;
using Content.Shared._Impstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Toolshed.Commands.Values;
using Content.Shared.Damage.Systems;

namespace Content.Client._Impstation.Administration.Systems;

public sealed class SwordDamoclesSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(SwordDamoclesAdded);
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentShutdown>(SwordDamoclesRemoved);
        // SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(TimesApplied);
    }

    private void SwordDamoclesRemoved(Entity<SwordDamoclesComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), SwordDamoclesKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private void SwordDamoclesAdded(Entity<SwordDamoclesComponent> ent, ref ComponentStartup args) //, Entity<SwordDamoclesComponent> ent
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

        // if (ent.Comp.TimesApplied >= 1)
        //     _damage.TryChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: true, interruptsDoAfters: true);

        // ent.Comp.TimesApplied += 1;
    }

    private enum SwordDamoclesKey
    {
        Key,
    }
}
