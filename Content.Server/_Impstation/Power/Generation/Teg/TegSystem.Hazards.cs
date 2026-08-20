using Content.Shared.Jittering;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Wires;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Jittering;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Repairable;
using Content.Server.Popups;
using Content.Shared._Impstation.ReagentEfficiency;
using Content.Shared._Impstation.Repairable;
using Content.Shared._Impstation.Damage;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly GasOutletInjectorSystem _gasInjectorSystem = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

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
    public bool IsOpen(Entity<TegCirculatorComponent> ent)
    {
        // Check the circulator component to avoid a TryComp.
        if (ent.Comp.Open != null)
            return (bool)ent.Comp.Open;

        // Try to get the WiresPanel component and see if it's open.
        var open = TryComp<WiresPanelComponent>(ent, out var panel) && panel.Open;

        // Populate the cache.
        ent.Comp.Open = open;
        return open;
    }

    /// <summary>
    /// Cancels the repair doafter if the circulator is not opened.
    /// </summary>
    private void OnRepairAttempt(Entity<TegCirculatorComponent> ent, ref RepairAttemptEvent args)
    {
        if (!IsOpen(ent))
        {
            // Cancel the attempt
            args.Cancelled = true;
            // Show popup
            _popup.PopupEntity(Loc.GetString("openable-component-try-use-closed", ("owner", ent)), ent, args.Repairer);
        }
    }

    private void UpdateCirculatorHazardAppearance(Entity<TegCirculatorComponent, ReagentEfficiencyComponent> ent, float efficiency, float stress)
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
        EfficiencyDamageComponent? effDamage = null;
        if (stress > 0f && ResolveEfficiencyDamage(ent, ref effDamage) && efficiency < effDamage.MinimumNominalEfficiency)
        {
            float amplitude = float.Lerp(10, 0, efficiency / effDamage.MinimumNominalEfficiency);
            float frequency = float.Lerp(60, 100, stress);
            _jitter.AddJitter(ent, amplitude, frequency);
        }
        else
            RemComp<JitteringComponent>(ent);
    }
}
