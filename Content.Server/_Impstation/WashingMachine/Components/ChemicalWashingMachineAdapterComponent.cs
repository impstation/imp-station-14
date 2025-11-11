using Content.Server._Impstation.WashingMachine.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;

/// <summary>
/// This is used for washing machines to handle reagent storage.
/// </summary>
[RegisterComponent, Access(typeof(WashingMachineSystem))]
public sealed partial class ChemicalWashingMachineAdapterComponent : Component
{
    /// <summary>
    /// A dictionary relating a reagent to accept as fuel input requred for a washing machine to run.
    /// 1 unit of this reagent will count as [float] units of fuel.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> ReagentWater = new();

    /// <summary>
    /// A dictionary relating reagent/s to accept as input that will clean all items in the cycle of dye.
    /// 1 unit of this reagent will count as [float] units of cleaner.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> ReagentCleaner = new();

    /// <summary>
    /// The name of <see cref="Solution"/>.
    /// </summary>
    [DataField("solution")]
    public string SolutionName = "tank";

    /// <summary>
    /// The solution on the <see cref="SolutionContainerManagerComponent"/> to use.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// How much reagent (can be fractional) is left in the tank.
    /// Stored in units of <see cref="FixedPoint2.Epsilon"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> FractionalReagents = new();
}
