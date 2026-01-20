using Content.Shared._Impstation.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
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
        var damageValue = FixedPoint2.New(args.TotalRads);

        DamageSpecifier damage = new();
        foreach (var typeId in ent.Comp.RadiationDamageCoefficients)
        {
            var adjustedDamage = damageValue * typeId.Value; // a negative value here will result in healing

            // Checks if clamps are setup and how to act on them
            var clampsDict = ent.Comp.RadiationDamageClamps;
            if (clampsDict != null && clampsDict.TryGetValue(typeId.Key, out var clamp))
            {
                // If the damage is greater than the clamp, we use the clamp instead
                if (adjustedDamage > 0 && adjustedDamage > clamp)
                    adjustedDamage = clamp;

                // If the healing is "greater" than the clamp, we use the clamp instead
                if (adjustedDamage < 0 && adjustedDamage < clamp)
                    adjustedDamage = clamp;
            }

            damage.DamageDict.Add(typeId.Key, adjustedDamage);
        }

        _damageable.ChangeDamage(ent.Owner, damage, interruptsDoAfters: false, origin: args.Origin);
    }
}
