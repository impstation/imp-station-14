using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Denotes the solution that can be easily removed through any reagent container.
///     Think pouring this or draining from a water tank.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrainableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    // imp add, load bearing milk thaven plush
    /// <summary>
    ///     if true will always drain from the provided solution.
    ///     otherwise ediblecomponent's solution will be used.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool OverrideEdibleSolution;
}
