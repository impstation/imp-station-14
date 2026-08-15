using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared._Impstation.ReagentEfficiency;
using Content.Shared.Damage.Components;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Impstation.Damage;

public sealed class EfficiencyDamageSystem : EntitySystem
{
    [Dependency]
    private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EfficiencyDamageComponent, ReagentEfficiencyTickEvent>(OnReagentEfficiencyTick);
    }

    private void OnReagentEfficiencyTick(Entity<EfficiencyDamageComponent> ent, ref ReagentEfficiencyTickEvent args)
    {
        ApplyEfficiencyDamage(ent, args);
    }

    /// <summary>
    /// Applies damage to the entity based on the given efficiency tick.
    /// </summary>
    /// <returns>The amount of damage incurred.</returns>
    private float ApplyEfficiencyDamage(Entity<EfficiencyDamageComponent> ent, ReagentEfficiencyTickEvent args)
    {
        // Ensure we have the damageableComponent
        if (!ResolveDamageable(ent, ref ent.Comp.DamageableComponentCache))
            return 0f;

        // No damage if the entity is running above its nominal efficiency.
        if (args.Efficiency > ent.Comp.MinimumNominalEfficiency)
            return 0f;

        // Calculate damage based on the efficiency, scaling linearly.
        float scaling = 1 - args.Efficiency / ent.Comp.MinimumNominalEfficiency; //TODO: ensure this calculation is correct
        float damage = float.Lerp(0, ent.Comp.MaxDamagePerTick, scaling);

        // Apply damage multiplier
        damage *= ent.Comp.DamageMultiplier; // TODO: this causes the damage dealt to go over *MaximumDamagePerTick*, which is not intuitive.

        // Ensure we don't deal negative damage by clamping
        damage = damage < 0f ? 0f : damage;

        // Apply the damage and return the amount dealt.
        Entity<DamageableComponent?> damageableEnt = (ent, ent.Comp.DamageableComponentCache);
        _damageable.TryChangeDamage(damageableEnt, ent.Comp.Damage * damage, ignoreResistances: true);
        return damage;
    }

    public bool ResolveDamageable(Entity<EfficiencyDamageComponent> ent, [NotNullWhen(true)] ref DamageableComponent? comp)
    {
        // Check if it's already not null
        // TODO: Ensure Comp.Owner == uid. Don't use .Owner directly though, it's depricated
        if (comp != null)
            return true;

        // Check cache
        if (ent.Comp.DamageableComponentCache != null)
        {
            comp = ent.Comp.DamageableComponentCache;
            return true;
        }

        // Try a normal Resolve
        if (Resolve(ent, ref comp))
        {
            // Found, update cache
            ent.Comp.DamageableComponentCache = comp;
            return true;
        }

        // This entity doesn't have the component
        return false;
    }

    public void SetDamageMultiplier(EfficiencyDamageComponent comp, float newDamageMultiplier)
    {
        comp.DamageMultiplier = newDamageMultiplier;
    }
}
