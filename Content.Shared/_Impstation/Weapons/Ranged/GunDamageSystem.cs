using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Applies gun damage to ammo projectiles
/// </summary>
public sealed class GunDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunDamageComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(EntityUid uid, GunDamageComponent component, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<ProjectileComponent>(projectile, out var proj))
                continue;

            if (component.OnlyGunDamage)
                proj.Damage = new DamageSpecifier(component.Damage);

            else
                proj.Damage += component.Damage;
        }
    }
}
