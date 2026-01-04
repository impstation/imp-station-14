using System.Numerics;
// using Content.Client.Administration.Components;
using Content.Shared._Impstation.Administration.Components;
// using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Toolshed.Commands.Values;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Impstation.Administration.Systems;

public sealed class SwordDamoclesSystem : EntitySystem
{
    // [Dependency] private readonly SpriteSystem _sprite = default!;

    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(SwordDamoclesAdded);
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentShutdown>(SwordDamoclesRemoved);
        // SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(TimesApplied);
    }

    private void SwordDamoclesRemoved(Entity<SwordDamoclesComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.TimesApplied = 0;
    }

    private void SwordDamoclesAdded(Entity<SwordDamoclesComponent> ent, ref ComponentStartup args) //, Entity<SwordDamoclesComponent> ent
    {
        if (ent.Comp.TimesApplied >= 1)
            _damage.TryChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: true, interruptsDoAfters: true);

        ent.Comp.TimesApplied += 1;
    }

    // private enum SwordDamoclesKey
    // {
    //     Key,
    // }
}
