using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Add/remove projectiles for guns that use ammo
/// </summary>
public sealed class GunProjectileCountModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunProjectileCountModifierComponent, GunGetAmmoProjectileCountEvent>(OnGetProjectileCount);
    }

    private void OnGetProjectileCount(EntityUid uid, GunProjectileCountModifierComponent component, ref GunGetAmmoProjectileCountEvent args)
    {
        args.Count += component.ProjCount;
    }
}
