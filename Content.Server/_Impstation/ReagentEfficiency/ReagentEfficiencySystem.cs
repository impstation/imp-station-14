using Content.Shared.Chemistry.EntitySystems;

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

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return 0f;

        // Calculate how much total solution should be removed from the container based on how full it is
        // Less full means less lubricant taken out
        // At a certain fill level the amount removed is constant (asymtotic?)

        // Remove the mixture amount

        // Find the total efficiency of all the reagents removed (weighted average)

        ent.Comp.PreviousEfficiency = 1f;
        return 1f;
    }
}
