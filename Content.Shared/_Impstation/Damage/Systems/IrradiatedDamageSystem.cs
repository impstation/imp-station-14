using Content.Shared._Impstation.Damage.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Radiation.Events;

namespace Content.Shared._Impstation.Damage.Systems;

public sealed partial class RadiationDamageModifierSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public const SlotFlags ProtectiveSlots =
        SlotFlags.FEET |
        SlotFlags.HEAD |
        SlotFlags.EYES |
        SlotFlags.GLOVES |
        SlotFlags.MASK |
        SlotFlags.NECK |
        SlotFlags.INNERCLOTHING |
        SlotFlags.OUTERCLOTHING;

    public override void Initialize()
    {
        SubscribeLocalEvent<IrradiatedDamageComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    /// <summary>
    /// Applies a damage change to an entity when irradiated based on the types defined in <see cref="IrradiatedDamageComponent"/>.
    /// This overrides the OnIrradiated behavior in <see cref="DamageableSystem.Events"/>.
    /// </summary>
    private void OnIrradiated(Entity<IrradiatedDamageComponent> ent, ref OnIrradiatedEvent args)
    {
        // We raise a query event to check the entity's damage modifiers from armor
        var armorQuery = new CoefficientQueryEvent(ProtectiveSlots);
        RaiseLocalEvent(ent, armorQuery);
        var radCoefficient = 1.0f;
        // If there's a damage modifier for the type we're looking for, we multiply the total rads received by this modifier
        if (armorQuery.DamageModifiers.Coefficients.TryGetValue(ent.Comp.ArmorProtectionType, out var armorCoefficient))
            radCoefficient = armorCoefficient;
        var effectiveRads = FixedPoint2.New(args.TotalRads) * radCoefficient;
        // If this were a more comprehensive rework for how taking damage from radiation works, the above code would probably be better as its own component like ZombificationResistanceComponent
        // Instead, we're piggybacking off of the existing radiation damage resistances from clothing to avoid a whole bunch of yaml changes

        var hasMobState = TryComp<MobStateComponent>(ent, out var mobState);
        DamageSpecifier damage = new();
        foreach (var typeId in ent.Comp.DamageCoefficients)
        {
            // Logic to be done if entity has a MobState
            if (hasMobState && mobState != null)
            {
                var allowedStatesDict = ent.Comp.AllowedTypesByState;
                // If a list of allowed types isn't set for the entity's current state, apply no health change for this damage type and continue the for loop
                if (!allowedStatesDict.TryGetValue(mobState.CurrentState, out var allowedTypes))
                    continue;

                // If the current mob state does not allow for the specified type, apply no health change for this damage type and continue the for loop
                if (!allowedTypes.Contains(typeId.Key))
                    continue;
            }

            var adjustedDamage = effectiveRads * typeId.Value; // a negative value here will result in healing

            // Checks if clamps are setup and how to act on them
            var limitsDict = ent.Comp.DamageLimits;
            if (limitsDict.TryGetValue(typeId.Key, out var limit))
            {
                // If the damage is greater than the clamp, we use the clamp instead
                if (adjustedDamage > 0 && adjustedDamage > limit)
                    adjustedDamage = limit;

                // If the healing is "greater" than the clamp, we use the clamp instead
                if (adjustedDamage < 0 && adjustedDamage < limit)
                    adjustedDamage = limit;
            }

            damage.DamageDict.Add(typeId.Key, adjustedDamage);
        }

        // We want to ignore resistances here, as that has already been taken care of by effectiveRads.
        _damageable.ChangeDamage(ent.Owner, damage, true, false, args.Origin);
    }
}
