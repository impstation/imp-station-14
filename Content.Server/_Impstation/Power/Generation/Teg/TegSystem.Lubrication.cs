using Content.Server._Impstation.ReagentEfficiency;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Wires;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    /// <summary>
    /// Name of the circulators' solution container.
    /// </summary>
    private const string SolutionName = "lube";

    /// <summary>
    /// The amount of time it takes to apply lubrication to the circulators.
    /// </summary>
    private const float RefillTimeSeconds = 2f;

    /// <summary>
    /// The maximum amount of lubrication that can be applied to the circulator in a single action.
    /// </summary>
    private const float MaxRefillAttemptAmount = 10f;

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

    private void AddSolutionAccessibility(EntityUid uid)
    {
        // RefillableSolutionComponent
        var refill = EnsureComp<RefillableSolutionComponent>(uid);
        refill.Solution = SolutionName;
        refill.RefillTime = TimeSpan.FromSeconds(RefillTimeSeconds);
        refill.MaxRefill = MaxRefillAttemptAmount;

        var draw = EnsureComp<DrawableSolutionComponent>(uid);
        draw.Solution = SolutionName;
    }

    private void RemoveSolutionAccessibility(EntityUid uid)
    {
        RemComp<RefillableSolutionComponent>(uid);
        RemComp<DrawableSolutionComponent>(uid);
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
    private float AverageCirculatorEfficiency(Entity<TegCirculatorComponent> circA, Entity<TegCirculatorComponent> circB, float δpA, float δpB, float dt)
    {
        //Get the ReagentEfficiencyComponents of each circulator
        ReagentEfficiencyComponent? recA = null;
        ReagentEfficiencyComponent? recB = null;
        if (!TryReagentEfficiencyComp(circA, ref recA) || !TryReagentEfficiencyComp(circA, ref recB))
        {
            // At least one of the circulators doesn't have the component.
            // Default to normal TEG behavior with 1f efficiency and no damage.
            return 1f;
        }

        // Calculate circulator stress based on delta p
        // At around 5000 dp, stress should be around 1.
        // Stress should scale infinitely, but far less than linearly.
        // https://www.desmos.com/calculator/jenpszfwix
        var stressA = δpA > 0 ? MathF.Log2(δpA + 1) / 12f : 0f;
        var stressB = δpB > 0 ? MathF.Log2(δpB + 1) / 12f : 0f;

        // Calculate efficiency boost from lubrication
        var (efficiencyA, consumedLubricantA) = CirculatorEfficiency(circA, dt, stressA);
        var (efficiencyB, consumedLubricantB) = CirculatorEfficiency(circB, dt, stressB);
        var averageCirculatorEfficiency = (efficiencyA + efficiencyB) / 2f;
        Log.Debug($"Efficiency cA: {efficiencyA} cB: {efficiencyB}");

        // Apply damage to the circulator based on its running efficiency.
        var damageA = ApplyCirculatorEfficiencyDamage(circA, efficiencyA, stressA);
        var damageB = ApplyCirculatorEfficiencyDamage(circB, efficiencyB, stressB);

        // See if we need to trigger a failure state
        CheckFail(circA, stressA);
        CheckFail(circB, stressB);

        // TODO: Apply any funny effects that specific reagents might have on the circulators.

        // Update appearances for different efficiencies and damages
        // TODO: make sure this doesn't have any problems bc the normal appearance updates are handled in the main teg update
        // TODO: Refactor lubricant processing to have a more uniform cache and access to relevant components, like solution
        _solution.ResolveSolution((EntityUid)circA, recA.SolutionName, ref recA.SolutionCache, out var solution);
        UpdateCirculatorHazardAppearance(circA, fillA, damageA, efficiencyA, stressA);
        UpdateCirculatorHazardAppearance(circB, fillB, damageB, efficiencyB, stressB);

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

    /// <summary>
    /// Tries to get the <see cref="ReagentEfficiencyComponent"/> associated with a TEG Circulator entity.
    /// </summary>
    /// <remarks>
    /// Uses the cache within <see cref="TegCirculatorComponent"/> before using Resolve().
    /// Bad pattern or design? idk
    /// </remarks>
    /// <param name="comp">A ref to the ReagentEfficiencyComponent. Expected to be null but not required.</param>
    /// <returns>Whether the entity has the ReagentEfficiencyComponent</returns>
    private bool TryReagentEfficiencyComp(Entity<TegCirculatorComponent> ent, ref ReagentEfficiencyComponent? comp)
    {
        // Check the cache in the circulator component
        if (ent.Comp._reagentEfficiencyComponentCache is not null)
        {
            comp = ent.Comp._reagentEfficiencyComponentCache;
            return true;
        }

        // Cache miss, check with Resolve.
        if (Resolve(ent, ref comp))
        {
            // Resolve success. Before returning, update the cache
            ent.Comp._reagentEfficiencyComponentCache = comp;
            return true;
        }

        // Component doesn't exist in the cache nor on the entity, so it doesn't exist.
        return false;
    }
}
