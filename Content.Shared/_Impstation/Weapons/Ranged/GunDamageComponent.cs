using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Lets guns that use ammo have its own damage value that's applied to its projectiles
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunDamageComponent : Component
{
    /// <summary>
    /// The damage value of the gun
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// If true, overrides ammo damage with gun damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnlyGunDamage = false;
}
