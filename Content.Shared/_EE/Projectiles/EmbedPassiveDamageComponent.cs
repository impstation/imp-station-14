using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared._EE.Projectiles;

/// <summary>
///   Passively damages the mob this embeddable is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmbedPassiveDamageComponent : Component
{
    /// <summary>
    ///   The entity this embeddable is attached to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Embedded = null;

    /// <summary>
    ///   The damage component to deal damage to.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public DamageableComponent? EmbeddedDamageable = null;

    /// <summary>
    ///   The MobState component to check if the target is still alive.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public MobStateComponent? EmbeddedMobState = null;

    /// <summary>
    ///   Damage per interval dealt to the entity every interval.
    ///   If this is set manually, DamageMultiplier will be ignored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    ///   Multiplier to be applied to the damage of DamageOtherOnHit to
    ///   calculate the damage per second.
    /// </summary>
    [DataField]
    public float ThrowingDamageMultiplier = 0.05f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDamage = TimeSpan.Zero;
}
