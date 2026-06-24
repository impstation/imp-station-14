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
        // Find nominal consumption
        var netConsumptionRate = ent.Comp.Consumption * consumptionMultiplier;
        var throttleThresholdVolume = (float)solution.MaxVolume * ent.Comp.ThrottlingThreshold;
        var nominalConsumption = dt * netConsumptionRate;

        // Find how much time the nominal consumption spent over the throttle threshold
        var nominalConsumptionOverThreshold = float.Clamp(throttleThresholdVolume - (float)solution.Volume - nominalConsumption, 0, nominalConsumption);
        var throttleDt = nominalConsumptionOverThreshold / netConsumptionRate;

        // Find amount consumed in throttle
        var throttledConsumption = Throttle((float)solution.Volume, netConsumptionRate, throttleDt);

        // Mix nominal consumption and throttled consumption amounts
        // let Cf = total consumption, Cn = nominal consumption, Ct = throttled consumption
        // When throttleDt = 0, Cf = Cn
        // When throttleDt = dt, Cf = Ct
        var consumedAmount = float.Lerp(nominalConsumption, throttledConsumption, throttleDt / dt);
        Log.Debug($"A0 {(float)solution.Volume}, r {netConsumptionRate}, Cn {nominalConsumption}, Cnover {nominalConsumptionOverThreshold}, tdt {throttleDt}, Ct {throttledConsumption}, Cf {consumedAmount}");

        var consumedSolution = _solution.SplitSolution(ent.Comp.SolutionCache.Value, consumedAmount);

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

        //Apply throttling to efficiency
        efficiency *= solution.Volume < throttleThresholdVolume ? (float)solution.Volume / throttleThresholdVolume : 1f; //Naiive and maybe not correct

        // Store and return calculated efficiency.
        ent.Comp.PreviousEfficiency = efficiency;
        return (efficiency, consumedSolution);
    }

    private float Throttle(float initialAmount, float consumptionRate, float dt)
    {
        // Find throttling amount: Stepwise function [0,threshold) linearly maps to [0,1). [threshold, inf) maps to 1.
        // var throttleThresholdVolume = solution.MaxVolume * comp.ThrottlingThreshold;
        // if (throttleThresholdVolume != 0f && solution.Volume >= throttleThresholdVolume)
        //     return 1f;

        // Naiive implementation:
        // // return (float)(solution.Volume / throttleThresholdVolume);
        // This equation takes a fraction of the volume based on the current volume.
        // This works as an approximation when dt is very small. But we do not have that guarantee.
        // When dt is very large, we will be consuming a lot more than intended.
        // For example, when solution.Volume is less than but very close to throttleThresholdVolume and dt is large,
        // we are consuming close to the unthrottled amount for a large portion of the tank.
        // Had dt been broken up into smaller chunks, we would have a lot more opportunities to process the correct amount.

        // Instead, we need to account for the consumption rate changing itself over the duration of its consumption dt.
        // This is functionally a continuously compounding interest, but instead of accumulating we are consuming.
        // So it's exponential decay instead of growth.
        // We can describe this problem with the following formula:
        // A(t) = (A_0)e^(rt)
        // Where
        //      A_0 is the initial (principal amount)   - The initial solution volume
        //      r is the rate of interest/consumption   - The rate of consumption, negative for us
        //      t is the elapsed time                   - dt
        //      A(t)                                    - The new solution volume after consumption

        // Return the amount consumed, not the new volume
        // A_0 - A(t)
        return initialAmount - initialAmount * MathF.Pow(MathF.E, -consumptionRate * dt);

        // // However, instead of a new final volume, we would like to return a value [0,1) as a consumption rate multiplier.
        // // Find the change in volume:
        // // dA = A_0 - A(t)
        // // Normalize by the initial value:
        // // throttle = (A_0 - A(t))/A_0
        // // Expand and Simplify:
        // // throttle = (A_0 - (A_0)e^(rt))/A_0
        // // -> A_0(1 - e^(rt))/A_0
        // // -> 1 - e^(rt)
        // return 1 - MathF.Pow(float.E, -1f * dt);
    }
}
