using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Wires;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    /// <summary>
    ///
    /// </summary>
    private const string SolutionName = "lube";

    /// <summary>
    ///
    /// </summary>
    private const float RefillTimeSeconds = 2f;

    /// <summary>
    ///
    /// </summary>
    private const float MaxRefillAttemptAmount = 10f;

    /// <summary>
    ///     Called when the WiresPanel component changes with the PanelChangedEvent.
    ///     Used for updating access to internal parts of a circulator like the lubricant (solution).
    /// </summary>
    private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    {
        //If opened, add passive vent
        if (args.Open)
        {
            AddSolutionAccessibility(uid);
        }

        //If closed, remove passive vent
        else
        {
            RemoveSolutionAccessibility(uid);
        }
    }

    private void AddSolutionAccessibility(EntityUid uid)
    {
        // RefillableSolutionComponent
        var refill = AddComp<RefillableSolutionComponent>(uid);
        refill.Solution = SolutionName;
        refill.RefillTime = TimeSpan.FromSeconds(RefillTimeSeconds);
        refill.MaxRefill = MaxRefillAttemptAmount;

        var examine = AddComp<ExaminableSolutionComponent>(uid);
        examine.Solution = SolutionName;

        var draw = AddComp<DrawableSolutionComponent>(uid);
        draw.Solution = SolutionName;
    }

    private void RemoveSolutionAccessibility(EntityUid uid)
    {
        RemComp<RefillableSolutionComponent>(uid);
        RemComp<ExaminableSolutionComponent>(uid);
        RemComp<DrawableSolutionComponent>(uid);
    }
}
