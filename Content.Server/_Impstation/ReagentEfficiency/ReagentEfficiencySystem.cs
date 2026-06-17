using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server._Impstation.ReagentEfficiency;

public sealed class ReagentEfficiencySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Consumes reagents stored in solution container and calculates the efficiency of the machine based on consumed solution.
    /// The efficiency is based on the types of reagents stored, their <see cref="ReagentEfficiencyComponent.Modifiers"/>, and the fullness of the solution.
    /// Updates <see cref="ReagentEfficiencyComponent.PreviousEfficiency"/> to match the return value of this function.
    /// </summary>
    /// <param name="dt">Time in seconds that has passed since the previous update.</param>
    /// <param name="consumptionMultiplier">
    ///     Multiplier for how much reagent will be consumed. Does not affect the value returned or written to PreviousEfficiency except for 0f, which returns 0f.
    /// </param>
    /// <returns>Machine efficiency as a float. Under 1.0 means substandard efficiency, over 1.0 means more efficient than normal.</returns>
    public (float, Solution) ApplyEfficiency(Entity<ReagentEfficiencyComponent?> ent, float dt, float consumptionMultiplier)
    {
        // Try to get the ReagentEfficiencyComponent.
        if (!Resolve(ent, ref ent.Comp))
            return (0f, new Solution());

        // If we are not consuming anything, the efficiency is 0.
        if (consumptionMultiplier <= 0f)
            return (0f, new Solution());

        // Try to get the Solution component.
        // Cacheable version of TryGetSolution
        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.Solution, ref ent.Comp.SolutionCache, out var solution))
            return (0f, new Solution());

        // If the solution is empty, the efficiency is 0.
        if (solution.Volume == FixedPoint2.Zero)
            return (0f, new Solution());

        // Remove the appropriate amount of solution.
        // Find throttling amount: Stepwise function [0,threshold) linearly maps to [0,1). [threshold, inf) maps to 1.
        var throttleThresholdVolume = solution.MaxVolume * ent.Comp.ThrottlingThreshold;
        var throttle = throttleThresholdVolume != 0f && solution.Volume < throttleThresholdVolume ? (float)(solution.Volume / throttleThresholdVolume) : 1f;
        // Scale amount removed by dt and consumption multiplier as well
        var consumedSolution = _solution.SplitSolution(ent.Comp.SolutionCache.Value, ent.Comp.Consumption * dt * throttle * consumptionMultiplier);

        // FixedPoint2 rounding WILL lead to small numbers becoming 0, affecting division down the line.
        if (consumedSolution.Volume == FixedPoint2.Zero)
            return (0f, consumedSolution);

        // Find the total modifier of all the reagents removed
        // Weighted average:
        //      Modifier * amount in removed solution
        //      Sum up proportional modifiers
        //      Divide by total volume consumed
        var efficiency = 0f;
        foreach (var reagent in consumedSolution.Contents)
        {
            efficiency += ent.Comp.Modifiers.TryGetValue(reagent.Reagent.Prototype, out var reagentEfficiency) ?
                reagentEfficiency * (float)reagent.Quantity :
                ent.Comp.DefaultModifier * (float)reagent.Quantity;
        }
        efficiency /= (float)consumedSolution.Volume;

        //Apply throttling
        efficiency *= throttle;

        // Store and return calculated efficiency.
        ent.Comp.PreviousEfficiency = efficiency;
        return (efficiency, consumedSolution);
    }
}
