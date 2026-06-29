using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.ReagentEfficiency;

/// <summary>
/// Makes a device's constant work more efficient by consuming reagents as it works.
/// A continuous version of <see cref="Shared.ReagentSpeed">.
/// </summary>
[RegisterComponent]
[Access(typeof(ReagentEfficiencySystem))]
public sealed partial class ReagentEfficiencyComponent : Component
{
    /// <summary>
    /// Solution that will be checked.
    /// Anything that isn't in <c>Modifiers</c> is left alone.
    /// </summary>
    [DataField(required: true)]
    public string SolutionName = string.Empty;

    /// <summary>
    /// Fill level at which consumption and efficiency throttling stops.
    /// When the solution volume is below this value, the amount of reagent consumed and efficiency decreases linearly to 0.
    /// </summary>
    [DataField]
    public float ThrottlingThreshold = 0.2f; //TODO: clamp to above 0

    /// <summary>
    /// How many units of reagent are consumed per second.
    /// </summary>
    [DataField]
    public float Consumption = 0.1f; //TODO: clamp to above 0

    /// <summary>
    /// Reagents and how lubricating they are. Higher values mean higher efficiency.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, float> Modifiers = new();

    [DataField]
    /// <summary>
    /// The default lubrication power for reagents not specified in the Modifiers list.
    /// </summary>
    public float DefaultModifier = 0.1f;

    /// <summary>
    /// The Efficiency calculated the last time <see cref="ReagentEfficiencySystem.ApplyEfficiency"/> was called.
    /// </summary>
    public float PreviousEfficiency = 0f;

    /// <summary>
    /// The solution consumed during the most recent tick when a solution was consumed.
    /// </summary>
    /// <remarks>
    /// Solutions may not be consumed every tick! <seealso cref="ConsumptionAccumulationThreshold"/>
    /// </remarks>
    public Solution PreviousConsumedSolution = new Solution();

    /// <summary>
    /// Solution component cache for <see cref="SharedSolutionContainerSystem.ResolveSolution"/>.
    /// </summary>
    public Entity<SolutionComponent>? SolutionCache = null;

    /// <summary>
    /// The amount of accumulated consumption before the solution is actually removed.
    /// </summary>
    /// <remarks>
    /// This is to ensure SOME amount of reagent is always being removed. If consumption drops to < 0.01u, it will be rounded down to 0 because of FixedPoint2's accuracy.
    /// </remarks>
    [DataField]
    public float ConsumptionAccumulationThreshold = 0.05f;

    /// <summary>
    /// When this value goes above <see cref="ConsumptionAccumulationThreshold"/>, the solution is actually removed from the container.
    /// </summary>
    public float CurrentConsumptionAccumulation = 0f; //TODO: Should be private with access for the system..?
}
