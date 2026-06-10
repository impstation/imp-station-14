using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Server._Impstation.ReagentEfficiency;

public sealed class ReagentEfficiencySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Consumes reagents stored in solution container and calculates the efficiency of the machine based on consumed solution.
    /// The efficiency is based on the types of reagents stored, their <see cref="ReagentEfficiencyComponent.Modifiers"/>, and the fullness of the solution.
    /// Updates <see cref="ReagentEfficiencyComponent.PreviousEfficiency"/> to match the return value of this function.
    /// </summary>
    /// <returns>Machine efficiency as a float. Under 1.0 means substandard efficiency, over 1.0 means more efficient than normal.</returns>
    public float ApplyEfficiency(Entity<ReagentEfficiencyComponent?> ent, float dt)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        // Cacheable version of TryGetSolution
        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.Solution, ref ent.Comp.SolutionCache, out var solution))
            return 0f;

        // Remove the mixture amount
        // Less full means less lubricant consumed
        // At a certain fill level the amount removed is constant (asymtotic?)
        var volume20Percent = solution.MaxVolume * 0.2;
        var fullnessMultiplier = volume20Percent != 0f && solution.Volume < volume20Percent ? (float)(solution.Volume / volume20Percent) : 1f; // TODO: less jank and magic numbers
        // Smaller dt means less lubricant consumed
        var consumedSolution = _solution.SplitSolution(ent.Comp.SolutionCache.Value, ent.Comp.Consumption * dt * fullnessMultiplier);

        // Find the total efficiency of all the reagents removed
        // Weighted average:
        //      Modifier * proportional amount in removed solution
        //      Sum up proportional modifiers
        //
        float efficiency = 0f;
        foreach (var reagent in consumedSolution.Contents)
        {
            efficiency += ent.Comp.Modifiers[reagent.Reagent.Prototype];
        }

        ent.Comp.PreviousEfficiency = 1f;
        return 1f;
    }
}
