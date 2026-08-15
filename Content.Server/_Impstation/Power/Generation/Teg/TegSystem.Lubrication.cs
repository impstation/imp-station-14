using System.Diagnostics.CodeAnalysis;
using Content.Shared._Impstation.ReagentEfficiency;
using Content.Shared._Impstation.Damage;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Wires;
using Robust.Shared.Toolshed.Commands.GameTiming;
using System.Linq;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly EfficiencyDamageSystem _efficiencyDamage = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly ReagentEfficiencySystem _reagentEfficiency = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    ///     Called when the WiresPanel component changes with the PanelChangedEvent.
    ///     Used for updating access to internal parts of a circulator like the lubricant (solution).
    ///     Also enables and disables the internal air injector.
    /// </summary>
    private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    {
        // Open the circulator. "Expose" the air injector to the atmosphere and allow reagent transfer.
        if (args.Open)
        {
            // AddSolutionAccessibility(uid);
            _openable.SetOpen(uid, true);
            ChangeInjectorState(uid, true);
        }

        // Close the circulator.
        else
        {
            // RemoveSolutionAccessibility(uid);
            _openable.SetOpen(uid, false);
            ChangeInjectorState(uid, false);
        }
    }

    /// <summary>
    /// Finds the average efficiency between both TEG circulators by calling <see cref="ReagentEfficiency.ApplyEfficiency"/> on both circulators.
    /// As a result, this function also causes the consumption of the lubricant in the circulators' solutions.
    /// This function also handles damage dealing to the circulators.
    /// This function also handles checking the failure state, potentially triggering it.
    /// This function also handles applying reagent special effects. (Not yet implemented)
    /// This function also handles updating the hazard visuals of the circulators.
    /// </summary>
    /// <remarks>
    /// TODO: This function does way too much and needs to be refactored or repurposed while still minimizing edits to the upstream generator update function.
    /// </remarks>
    /// <param name="circA">The first circulator</param>
    /// <param name="circB">The second circulator</param>
    /// <param name="δpA">The delta pressure experienced by the first circulator</param>
    /// <param name="δpB">The delta pressure experienced by the second circulator</param>
    /// <param name="dt">The time since the last update.</param>
    /// <returns>The average efficiency of both circulators.</returns>
    private float AverageCirculatorEfficiency(Entity<TegCirculatorComponent, ReagentEfficiencyComponent?> circA, Entity<TegCirculatorComponent, ReagentEfficiencyComponent?> circB, float δpA, float δpB, float dt)
    {
        // Get the ReagentEfficiencyComponents of each circulator
        if (!ResolveReagentEfficiency(circA, ref circA.Comp2) || !ResolveReagentEfficiency(circB, ref circB.Comp2))
        {
            // At least one of the circulators doesn't have the component.
            // Default to normal TEG behavior with 1f efficiency and no damage.
            return 1f;
        }

        // Get the efficiency damage components. It's ok if they don't exist
        EfficiencyDamageComponent? effDamageA = null, effDamageB = null;
        ResolveEfficiencyDamage(circA, ref effDamageA);
        ResolveEfficiencyDamage(circB, ref effDamageB);

        // Create new circulator entities with the components we need
        var entA = new Entity<TegCirculatorComponent, ReagentEfficiencyComponent, EfficiencyDamageComponent?>(circA, circA.Comp1, circA.Comp2, effDamageA);
        var entB = new Entity<TegCirculatorComponent, ReagentEfficiencyComponent, EfficiencyDamageComponent?>(circB, circB.Comp1, circB.Comp2, effDamageB);

        // Calculate circulator stress based on delta p
        var stressA = DeltaPToStress(δpA);
        var stressB = DeltaPToStress(δpB);

        // Apply damage multiplier to each circ based on stress
        if (entA.Comp3 != null)
            _efficiencyDamage.SetDamageMultiplier(entA.Comp3, stressA);
        if (entB.Comp3 != null)
            _efficiencyDamage.SetDamageMultiplier(entB.Comp3, stressB);

        // Calculate efficiency multiplier from lubrication
        var (efficiencyA, consumedLubricantA) = CirculatorEfficiency(entA, dt, stressA);
        var (efficiencyB, consumedLubricantB) = CirculatorEfficiency(entB, dt, stressB);
        var averageCirculatorEfficiency = (efficiencyA + efficiencyB) / 2f;
        // Log.Debug($"Efficiency cA: {efficiencyA} cB: {efficiencyB}");

        // TODO: Apply any funny effects that specific reagents might have on the circulators.

        // Update appearances for different efficiencies and damages
        UpdateCirculatorHazardAppearance(entA, efficiencyA, stressA);
        UpdateCirculatorHazardAppearance(entB, efficiencyB, stressB);

        return averageCirculatorEfficiency;
    }

    /// <summary>
    /// Calculates the efficiency of each circulator using their lubricant solutions.
    /// Consumption ramps up as the circulatorRate increases.
    /// </summary>
    /// <param name="dt">The amount of time since the last efficiency calculation.</param>
    /// <param name="circulatorStress">The speed the circulator is running at.</param>
    /// <returns></returns>
    private (float, Solution) CirculatorEfficiency(EntityUid uid, float dt, float circulatorStress)
    {
        // Do nothing if there's no gas flow
        // TODO: this causes a desync with the component, this returns 1 but the component's PreviousEfficiency isn't updated. Remove this
        if (circulatorStress == 0)
            return (1f, new Solution());

        return _reagentEfficiency.ApplyEfficiency(uid, dt, circulatorStress);
    }

    private float DeltaPToStress(float δp)
    {
        // At around 5000 dp, stress should be around 1.
        // Stress should scale infinitely, but far less than linearly.
        // https://www.desmos.com/calculator/jenpszfwix
        return δp > 0 ? MathF.Log2(δp + 1) / 12f : 0f;
    }

    /// <summary>
    /// Tries to get the <see cref="ReagentEfficiencyComponent"/> associated with a TEG Circulator entity.
    /// </summary>
    /// <remarks>
    /// Uses the cache within <see cref="TegCirculatorComponent"/> before using Resolve().
    /// Bad pattern or design? idk
    /// </remarks>
    /// <param name="comp">A ref to the ReagentEfficiencyComponent. Expected to be null but not required.</param>
    /// <returns>Whether the entity has the ReagentEfficiencyComponent</returns>
    private bool ResolveReagentEfficiency(Entity<TegCirculatorComponent> ent, [NotNullWhen(true)] ref ReagentEfficiencyComponent? comp)
    {
        // Check the cache in the circulator component
        if (ent.Comp.ReagentEfficiencyComponentCache is not null)
        {
            comp = ent.Comp.ReagentEfficiencyComponentCache;
            return true;
        }

        // Cache miss, check with Resolve.
        if (Resolve(ent, ref comp, logMissing: false))
        {
            // Resolve success. Before returning, update the cache
            ent.Comp.ReagentEfficiencyComponentCache = comp;
            return true;
        }

        // Component doesn't exist in the cache nor on the entity, so it doesn't exist.
        return false;
    }

    /// <summary>
    /// Tries to get the <see cref="EfficiencyDamageComponent"/> associated with a TEG Circulator entity.
    /// </summary>
    /// <remarks>
    /// Uses the cache within <see cref="TegCirculatorComponent"/> before using Resolve().
    /// Bad pattern or design? idk
    /// </remarks>
    /// <param name="comp">A ref to the ReagentEfficiencyComponent. Expected to be null but not required.</param>
    /// <returns>Whether the entity has the ReagentEfficiencyComponent</returns>
    private bool ResolveEfficiencyDamage(Entity<TegCirculatorComponent> ent, [NotNullWhen(true)] ref EfficiencyDamageComponent? comp)
    {
        // Check if comp is already not null
        // TODO: check if comp's owner is ent
        if (comp != null)
            return true;

        // Check the cache in the circulator component
        if (ent.Comp.EfficiencyDamageComponentCache is not null)
        {
            comp = ent.Comp.EfficiencyDamageComponentCache;
            return true;
        }

        // Cache miss, check with Resolve.
        if (Resolve(ent, ref comp, logMissing: false))
        {
            // Resolve success. Before returning, update the cache
            ent.Comp.EfficiencyDamageComponentCache = comp;
            return true;
        }

        // Component doesn't exist in the cache nor on the entity, so it doesn't exist.
        return false;
    }

    /// <summary>
    /// Gets the lubricant fill level of the circulator as a fraction of the max volume.
    /// </summary>
    /// <returns>The fill level in a range [0,1]. If a <see cref="SolutionComponent"/> is not found, 0f is returned.</returns>
    private float GetCirculatorFillLevel(Entity<TegCirculatorComponent, ReagentEfficiencyComponent> circ)
    {
        // Get the solution and ensure it exists.
        if (!_solution.ResolveSolution((EntityUid)circ, circ.Comp2.SolutionName, ref circ.Comp2.SolutionCache, out var solution))
            return 0f;

        // Ensure we don't divide by zero.
        if (solution.MaxVolume == 0)
            return 0f;

        // Find the fill level
        return (float)(solution.Volume / solution.MaxVolume);
    }
}
