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
        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.SolutionCache, out var solution))
            return (0f, new Solution());

        // If the solution is empty, the efficiency is 0.
        if (solution.Volume == FixedPoint2.Zero)
            return (0f, new Solution());

        // Remove the appropriate amount of solution.
        // Find nominal consumption
        var consumptionRate = ent.Comp.Consumption * consumptionMultiplier;
        var throttleThresholdVolume = (float)solution.MaxVolume * ent.Comp.ThrottlingThreshold;
        var nominalConsumption = dt * consumptionRate;

        // Find how much time the nominal consumption spent over the throttle threshold
        var nominalConsumptionOverThreshold = float.Clamp(throttleThresholdVolume - (float)solution.Volume - nominalConsumption, 0, nominalConsumption);
        var throttleDt = nominalConsumptionOverThreshold / consumptionRate;

        // Find amount consumed in throttle
        var throttledConsumption = Throttle((float)solution.Volume - ent.Comp.CurrentConsumptionAccumulation, consumptionRate, throttleDt, throttleThresholdVolume);

        // Mix nominal consumption and throttled consumption amounts
        // let Cf = total consumption, Cn = nominal consumption, Ct = throttled consumption
        // When throttleDt = 0, Cf = Cn
        // When throttleDt = dt, Cf = Ct
        var consumedAmount = float.Lerp(nominalConsumption, throttledConsumption, throttleDt / dt);
        Log.Debug($"A0 {(float)solution.Volume}, r {consumptionRate}, Cn {nominalConsumption}, Cnover {nominalConsumptionOverThreshold}, tdt {throttleDt}, Ct {throttledConsumption}, Cf {consumedAmount}");


        // Accumulate consumption if necessary.
        Solution consumedSolution;
        if (consumedAmount + ent.Comp.CurrentConsumptionAccumulation < ent.Comp.ConsumptionAccumulationThreshold)
        {
            // Consumption this tick is too low, accumulate it and use the previously consumed solution.
            consumedSolution = solution; //TODO: this logic may be flawed if it's used to calculate "metabolism" of reagents. We'll be returning a lot more reagents than we actually consume.
            ent.Comp.CurrentConsumptionAccumulation += consumedAmount;
            Log.Debug($"Consumption too low: accumulated to {ent.Comp.CurrentConsumptionAccumulation}");
        }
        else
        {
            // Consumption and accumulation exceed the threshold and can be processed this tick.
            consumedSolution = _solution.SplitSolution(ent.Comp.SolutionCache.Value, consumedAmount + ent.Comp.CurrentConsumptionAccumulation);
            ent.Comp.CurrentConsumptionAccumulation = 0f;
        }

        // Just for sanity, ensure that the consumed amount is more than 0.
        if (consumedAmount + ent.Comp.CurrentConsumptionAccumulation == 0f)
            return (0f, new Solution());

        // Find the total modifier of all the reagents removed
        // We calculate based on what's in the full solution, not the removed solution to lessen FixedPoint2 rounding erorrs.
        // Weighted average:
        //      Modifier * amount of reagent
        //      Sum up proportional modifiers
        //      Divide by total volume consumed
        var efficiency = 0f;
        foreach (var reagent in solution)
        {
            efficiency += ent.Comp.Modifiers.TryGetValue(reagent.Reagent.Prototype, out var reagentEfficiency) ?
                reagentEfficiency * (float)reagent.Quantity :
                ent.Comp.DefaultModifier * (float)reagent.Quantity;
        }
        efficiency /= (float)solution.Volume;
        // efficiency /= consumedAmount + ent.Comp.CurrentConsumptionAccumulation;

        //Apply throttling to efficiency
        efficiency *= solution.Volume < throttleThresholdVolume ? (float)solution.Volume / throttleThresholdVolume : 1f;

        // Store and return calculated efficiency.
        ent.Comp.PreviousEfficiency = efficiency;
        return (efficiency, consumedSolution);
    }

    private float Throttle(float initialAmount, float consumptionRate, float dt, float throttleThresholdVolume)
    {
        // Naiive implementation:
        // // return (float)(solution.Volume / throttleThresholdVolume);
        // This equation takes a fraction of the volume based on the current volume.
        // This works as an approximation when dt is very small. But we do not have that guarantee.
        // When dt is very large, we will be consuming a lot more than intended.
        // For example, when solution.Volume is less than but very close to throttleThresholdVolume and dt is large,
        // we are consuming close to the unthrottled amount for a large portion of the tank.
        // Had dt been broken up into smaller chunks, we would have a lot more opportunities to process the correct amount.

        // Protect against division by 0 when calculating m:
        if (dt == 0f)
            return 0f;

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

        // Find the amount consumed, not the new volume. Just subtract the new volume from the original
        // A_0 - A(t)
        var consumedAmount = initialAmount - initialAmount * MathF.Pow(MathF.E, -consumptionRate * dt);

        // // https://www.desmos.com/calculator/njoz37ate9
        // We would like the throttle consumption to be equal to the nominal consumption when the volume (A_0) is at the throttle threshold volume.
        // For this problem, A_0 is our independent variable, so we'll call it x.
        // Let's assume t is constant for this because we've already accounted for its variability in the previous problem.
        // Our nominal consumption is Cn = rt, which is independent of volume and is therefore a constant function.
        // Let h be our throttle threshold volume.
        // Our throttled consumption is                             Ct = x - x * e^(-rt)
        // We can add a multiplier m to Ct to change its slope:     mCt = m(x - x * e^(-rt))
        // We want mCt = Cn when x = h:                             m(h - h * e^(-rt)) = rt
        // Solve for m:                                             m = rt/(h - h * e^(-rt))
        // mCt is our final solution.
        var m = consumptionRate * dt / (throttleThresholdVolume - throttleThresholdVolume * MathF.Pow(MathF.E, -consumptionRate * dt));
        return m * consumedAmount;
    }
}
