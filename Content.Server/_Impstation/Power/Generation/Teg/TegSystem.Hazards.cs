using Content.Server.Atmos.Piping.Components;
using Content.Shared.Wires;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly GasOutletInjectorSystem _gasInjectorSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    /// <summary>
    /// Changes the state of the air injector given a bool.
    /// </summary>
    /// <param name="state">true for enabled, false for disabled</param>
    private void ChangeInjectorState(EntityUid uid, bool state)
    {
        if (!TryComp<GasOutletInjectorComponent>(uid, out var injector))
            return;

        _gasInjectorSystem.SetEnabled(uid, state, injector);
    }

    /// <returns>Whether this circulator is open.</returns>
    public bool IsOpen(EntityUid uid)
    {
        // Try to get the WiresPanel component and see if it's open.
        return TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open;
    }

    /// <summary>
    /// Applies damage to the circulator based on the efficiency.
    /// </summary>
    /// <param name="efficiency">The efficiency of the circulator. Probably calculated by <see cref="ReagentEfficiencySystem"/>.</param>
    /// <returns>The amount of damage incurred.</returns>
    private float ApplyCirculatorEfficiencyDamage(Entity<TegCirculatorComponent?> ent, float efficiency, float stress)
    {
        // Ensure the circulator component exists.
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        // No damage if the circulator is running above its nominal efficiency.
        if (efficiency > ent.Comp.MinimumNominalEfficiency)
            return 0f;

        // No damage if the circulator is not running
        if (stress == 0f)
            return 0f;

        // Calculate damage based on the efficiency, scaling linearly.
        float scaling = 1 - efficiency / ent.Comp.MinimumNominalEfficiency;
        float damage = float.Lerp(0, ent.Comp.MaximumDamagePerTick, scaling);

        // Scale damage based on stress. More gas flow means more damage
        damage *= stress;

        // Apply the damage and return the amount dealt.
        ent.Comp.Integrity -= damage;
        return damage;
    }

    private void CheckFail(Entity<TegCirculatorComponent?> ent, float stress)
    {
        // Ensure the circulator component exists.
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Do nothing if there is still integrity
        if (ent.Comp.Integrity > 0)
            return;

        // Do nothing if the circulator isn't running
        if (stress < 0.001f)
            return;

        // Pass to failure mode
        Explode(ent, stress);
    }

    private void Explode(EntityUid ent, float stress)
    {
        _explosionSystem.TriggerExplosive(ent, radius: 10);
    }
}
