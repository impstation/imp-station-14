using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Content.Shared._EE.Damage.Systems; // EE THROWING
using Robust.Shared.GameStates; // EE THROWING

namespace Content.Shared.Damage.Components;

/// <summary>
/// Makes this entity deal damage when thrown at something.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState] // imp add
[Access(typeof(SharedDamageOtherOnHitSystem), typeof(EEThrowingSystem), typeof(SharedProjectileSystem))] // imp add EEThrowingSystem, SharedProjectileSystem
public sealed partial class DamageOtherOnHitComponent : Component
{
    /// <summary>
    /// Whether to ignore damage modifiers.
    /// </summary>
    [DataField, AutoNetworkedField] //  IMP: add networked
    public bool IgnoreResistances = false;

    /// <summary>
    /// The damage amount to deal on hit.
    ///     IMP: If this is empty, damage will be inherited from <see cref="MeleeWeaponComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField] //  IMP: remove required true, add networked
    public DamageSpecifier Damage = new(); // IMP: damage is empty, not default!

    // EE THROWING START

    /// <summary>
    ///   The stamina cost of throwing this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StaminaCost = 3.5f;

    /// <summary>
    ///   The maximum amount of hits per throw before landing on the floor.
    /// </summary>
    [DataField]
    public int MaxHitQuantity = 1;

    /// <summary>
    ///   The multiplier to apply to the entity's light attack damage to calculate the throwing damage.
    ///   Only used if this component has a MeleeWeaponComponent and Damage is not set on the prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MeleeDamageMultiplier = 1f;

    /// <summary>
    ///   The minimum velocity required to deal damage.
    /// </summary>
    [DataField]
    public float MinVelocity = 1f;

    /// <summary>
    ///   The sound to play when this entity hits on a throw.
    ///   If null, attempts to retrieve the HitSound from MeleeWeaponComponent.
    /// </summary>
    [DataField("soundHit")]
    public SoundSpecifier? HitSound;

    /// <summary>
    ///   Plays if no damage is done to the target entity.
    ///   If null, attempts to retrieve the NoDamageSound from MeleeWeaponComponent.
    /// </summary>
    [DataField]
    public SoundSpecifier? NoDamageSound;

    // EE END
}
