using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared._Impstation.Weapons.Ranged.Components;

namespace Content.Shared._Impstation.Weapons.Ranged.Systems;

/// <summary>
/// Adds damage for guns that use cartridge-based projectiles.
/// </summary>
public sealed class GunDamageSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

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

            var damageToApply = component.Damage;

            foreach (var (tag, damage) in component.DamageSpecific)
            {
                if (_tagSystem.HasTag(projectile, tag))
                {
                    damageToApply = damage;
                    break;
                }
            }

            if (component.OnlyGunDamage)
                proj.Damage = new DamageSpecifier(damageToApply);
            else
                proj.Damage += damageToApply;
        }
    }
}
