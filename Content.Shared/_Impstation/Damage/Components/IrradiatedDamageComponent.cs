using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Radiation.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Damage.Components;

/// <summary>
/// Allows an entity to have finer control than <see cref="DamageableComponent"/> can over how it's damage is changed when irradiated.
/// An entity with this component will override OnIrradiated behavior in <see cref="DamageableSystem.Events"/>.
/// This works similarly to <see cref="PassiveDamageComponent"/>, but is only invoked on <see cref="OnIrradiatedEvent"/>.
/// </summary>
/// <remarks>
/// The default values in this component are to mimic the default behavior of radiation in DamageableComponent.
/// </remarks>
[NetworkedComponent, RegisterComponent]
public sealed partial class IrradiatedDamageComponent : Component
{
    /// <summary>
    /// The types and/or groups of damage <see cref="OnIrradiatedEvent"/> causes to the entity and a coefficient for damage per rad.
    /// A negative coefficient will cause irradiation to heal the specified type.
    /// By default, the radiation.gridcast.update_rate CVAR is one second.
    /// This effectively makes damage/healing per second an entity's TotalRads * RadiationDamageCoefficients, capped by RadiationDamageClamps.
    /// </summary>
    /// <remarks>
    /// This could maybe be a DamageModifierSet, but this system doesn't use the FlatReduction of damage modifiers.
    /// </remarks>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> DamageCoefficients = new()
    {
        {"Radiation", 1.0 }
    };

    /// <summary>
    /// The valid mob states for damage to be applied to an entity per damage type.
    /// For example, this could let an entity heal brute damage only when alive while still causing radiation damage when dead.
    /// If an entity does not have a <see cref="MobState"/>, the contents of this dictionary are ignored.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, List<MobState>> AllowedStates = new()
    {
        { "Radiation", new List<MobState> {MobState.Alive, MobState.Critical, MobState.Dead} }
    };

    /// <summary>
    /// The maximum damage change per <see cref="OnIrradiatedEvent"/> for the specified damage types.
    /// A negative value here will limit healing.
    /// If a type is missing or is set to 0, damage per <see cref="OnIrradiatedEvent"/> will not have an upper limit
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> DamageLimits = new();
}
