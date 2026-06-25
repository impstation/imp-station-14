using Content.Server._Impstation.ReagentEfficiency;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Wires;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    /// <summary>
    /// Name of the circulators' solution container.
    /// </summary>
    private const string SolutionName = "lube";

    /// <summary>
    /// The amount of time it takes to apply lubrication to the circulators.
    /// </summary>
    private const float RefillTimeSeconds = 2f;

    /// <summary>
    /// The maximum amount of lubrication that can be applied to the circulator in a single action.
    /// </summary>
    private const float MaxRefillAttemptAmount = 10f;

    [Dependency] private readonly ReagentEfficiencySystem _reagentEfficiency = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;

    /// <summary>
    ///     Called when the WiresPanel component changes with the PanelChangedEvent.
    ///     Used for updating access to internal parts of a circulator like the lubricant (solution).
    ///     Also enables and disables the internal air injector.
    /// </summary>
    private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    {
        // Open the circulator. "Expose" the air injector to the atmosphere and allow reagent transfer.
        if (args.Open)
        {
            // AddSolutionAccessibility(uid);
            _openable.SetOpen(uid, true);
            ChangeInjectorState(uid, true);
        }

        // Close the circulator.
        else
        {
            // RemoveSolutionAccessibility(uid);
            _openable.SetOpen(uid, false);
            ChangeInjectorState(uid, false);
        }
    }

    private void AddSolutionAccessibility(EntityUid uid)
    {
        // RefillableSolutionComponent
        var refill = EnsureComp<RefillableSolutionComponent>(uid);
        refill.Solution = SolutionName;
        refill.RefillTime = TimeSpan.FromSeconds(RefillTimeSeconds);
        refill.MaxRefill = MaxRefillAttemptAmount;

        var draw = EnsureComp<DrawableSolutionComponent>(uid);
        draw.Solution = SolutionName;
    }

    private void RemoveSolutionAccessibility(EntityUid uid)
    {
        RemComp<RefillableSolutionComponent>(uid);
        RemComp<DrawableSolutionComponent>(uid);
    }

    /// <summary>
    /// Calculates the efficiency of each circulator using their lubricant solutions.
    /// Consumption ramps up as the circulatorRate increases.
    /// </summary>
    /// <param name="dt">The amount of time since the last efficiency calculation.</param>
    /// <param name="circulatorRate">The speed the circulator is running at.</param>
    /// <returns></returns>
    private (float, Solution) CirculatorEfficiency(EntityUid uid, float dt, float circulatorRate)
    {
        // At around 5000 rate, consumption modifier should be around 1.
        // Consumption should scale infinitely, but far less than linearly.
        // https://www.desmos.com/calculator/myeflomtaz
        var consumptionModifier = circulatorRate > 0 ? MathF.Log2(circulatorRate) / 12f : 0f;
        return _reagentEfficiency.ApplyEfficiency(uid, dt, consumptionModifier);
    }
}
