using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.ReagentEfficiency;

/// <summary>
/// Makes a device's constant work more efficient by consuming reagents as it works.
/// A continuous version of <see cref="Shared.ReagentSpeed">.
/// </summary>
[RegisterComponent]
public sealed partial class ReagentEfficiencyComponent : Component
{
    /// <summary>
    /// Solution that will be checked.
    /// Anything that isn't in <c>Modifiers</c> is left alone.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// How many units of reagent are consumed per second.
    /// </summary>
    [DataField]
    public float Consumption = 1f;

    /// <summary>
    /// Reagents and how lubricating they are. Higher values mean higher efficiency.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, float> Modifiers = new();

    [DataField]
    /// <summary>
    /// The default lubrication power for reagents not specified in the Modifiers list.
    /// </summary>
    public float DefaultModifier = 0.3f;

    /// <summary>
    /// The Efficiency calculated the last time <see cref="ReagentEfficiencySystem.ApplyEfficiency"/> was called.
    /// </summary>
    public float PreviousEfficiency = 0f;

    /// <summary>
    /// Solution component cache.
    /// </summary>
    public Entity<SolutionComponent>? SolutionCache = null;
}
