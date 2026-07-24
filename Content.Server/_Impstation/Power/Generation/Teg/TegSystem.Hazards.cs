using Content.Shared.Jittering;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Wires;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Jittering;
using Content.Server._Impstation.ReagentEfficiency;


namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly GasOutletInjectorSystem _gasInjectorSystem = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;

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
        return TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open; //TODO: Optimize out this trycomp
    }

    /// <summary>
    /// Applies damage to the circulator based on the efficiency.
    /// </summary>
    /// <param name="efficiency">The efficiency of the circulator. Probably calculated by <see cref="ReagentEfficiencySystem"/>.</param>
    /// <returns>The amount of damage incurred.</returns>
    private float ApplyCirculatorEfficiencyDamage(Entity<TegCirculatorComponent> ent, float efficiency, float stress)
    {
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

    private void CheckFail(Entity<TegCirculatorComponent> ent, float stress)
    {
        // Do nothing if there is still integrity
        if (ent.Comp.Integrity > 0)
            return;

        // Do nothing if the circulator isn't running
        if (stress < 0.001f)
            return;

        // Pass to failure mode
        Explode(ent, stress);
    }

    private void Explode(Entity<TegCirculatorComponent> ent, float stress)
    {
        float radius = float.Lerp(ent.Comp.ExplosionRadiusRange.Item1, ent.Comp.ExplosionRadiusRange.Item2, stress);
        Log.Debug($"Explosion triggered. Stress {stress}, radius {radius}");
        _explosionSystem.TriggerExplosive(ent, radius: radius);
    }

    private void UpdateCirculatorHazardAppearance(Entity<TegCirculatorComponent, ReagentEfficiencyComponent> ent, float damageTaken, float efficiency, float stress)
    {
        // Apply fill level visual
        var fillLevelEnum = GetCirculatorFillLevel(ent) switch
        {
            >= 0.2f => TegFillLevel.Nominal,
            >= 0.05f and < 0.2f => TegFillLevel.Warning,
            _ => TegFillLevel.Subnominal
        };
        _appearance.SetData(ent, TegVisuals.CirculatorFillLevel, fillLevelEnum);

        // Apply subnominal visuals if taking damage
        // TODO: Jittering uses so many component lookups. Optimize or remove this wholesale.
        // TODO: Could look better
        if (efficiency < ent.Comp1.MinimumNominalEfficiency)
        {
            float amplitude = float.Lerp(10, 0, efficiency / ent.Comp1.MinimumNominalEfficiency);
            float frequency = float.Lerp(20, 80, stress);
            _jitter.AddJitter(ent, amplitude, frequency);
        }
        else
            RemComp<JitteringComponent>(ent);
    }
}
