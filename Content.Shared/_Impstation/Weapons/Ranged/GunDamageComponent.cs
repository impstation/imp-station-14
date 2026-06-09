using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Weapons.Ranged.Components;

/// <summary>
/// Sets a guns damage values independently of its ammunition.
/// By default, without this component, a gun's ammunition is the only factor in the amount of damage it deals.
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
    /// The chance for the damage modifier to be applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageChance = 1f;

    /// <summary>
    /// If true, overrides ammo damage with gun damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnlyGunDamage = false;
}
