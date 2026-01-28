using Content.Shared._Impstation.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Radiation.Events;

namespace Content.Shared._Impstation.Damage.Systems;

public sealed partial class RadiationDamageModifierSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
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
        var hasMobState = TryComp<MobStateComponent>(ent, out var mobState);
        var damageValue = FixedPoint2.New(args.TotalRads);
        //TODO: Adjust damageValue by RadiationPPE component

        DamageSpecifier damage = new();
        foreach (var typeId in ent.Comp.DamageCoefficients)
        {
            //Logic to be done if entity has a MobState
            if (hasMobState && mobState != null)
            {
                var allowedStatesDict = ent.Comp.AllowedTypesByState;
                //If a list of allowed types isn't set for the entity's current state, apply no health change for this damage type and continue the for loop
                if (!allowedStatesDict.TryGetValue(mobState.CurrentState, out var allowedTypes))
                    continue;

                //If the current mob state does not allow for the specified type, apply no health change for this damage type and continue the for loop
                if (!allowedTypes.Contains(typeId.Key))
                    continue;
            }

            var adjustedDamage = damageValue * typeId.Value; // a negative value here will result in healing

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

        _damageable.ChangeDamage(ent.Owner, damage, true, false, args.Origin);
    }
}
