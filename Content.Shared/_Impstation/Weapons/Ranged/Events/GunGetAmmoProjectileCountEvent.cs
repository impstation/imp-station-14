namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Event for guns to modify ammo projectile count
/// </summary>
[ByRefEvent]
public record struct GunGetAmmoProjectileCountEvent(int Count);
