using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Chemistry.Components;

/// <summary>
///     Denotes the solution that can be easily removed through any reagent container.
///     Think pouring this or draining from a water tank.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionFuelDrainComponent : Component
{
    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [DataField(required: true)]
    public string Solution = default!;
}
