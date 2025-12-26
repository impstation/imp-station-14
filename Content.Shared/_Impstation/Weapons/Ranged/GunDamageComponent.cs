using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Lets guns that use ammo have its own damage value that's applied to its projectiles.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunDamageComponent : Component
{
    /// <summary>
    /// The default damage value of the gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Checks for if the projectile has a tag, if it does then it deals whatever damage is defined instead of the default.
    /// Use this for different ammo types.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, DamageSpecifier> DamageSpecific = new();

    /// <summary>
    /// If true, overrides ammo damage with gun damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnlyGunDamage = false;
}
