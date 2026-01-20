using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Radiation.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Damage.Components;

/// <summary>
/// Allows an entity to have finer control than <see cref="DamageableComponent"/> can over how it's damage is changed when irradiated.
/// An entity with this component will override OnIrradiated behavior in <see cref="DamageableSystem.Events"/>.
/// This is its own component to minimize changes to usptream files.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class IrradiatedDamageComponent : Component
{
    /// <summary>
    /// The types of damage <see cref="OnIrradiatedEvent"/> causes to the entity and a coefficient for damage per rad.
    /// A negative coefficient will cause irradiation to heal the specified type.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> RadiationDamageCoefficients = new()
    {
        {"Radiation", 1.0 }
    };

    /// <summary>
    /// The maximum damage change per <see cref="OnIrradiatedEvent"/> for the specified damage type.
    /// A negative value here will limit healing.
    /// If left null, damage per <see cref="OnIrradiatedEvent"/> will not have an upper limit
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>? RadiationDamageClamps;
}
