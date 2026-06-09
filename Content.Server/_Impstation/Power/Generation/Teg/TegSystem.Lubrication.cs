using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
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

    /// <summary>
    ///     Called when the WiresPanel component changes with the PanelChangedEvent.
    ///     Used for updating access to internal parts of a circulator like the lubricant (solution).
    ///     Also enables and disables the internal air injector.
    /// </summary>
    private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    {
        if (args.Open)
        {
            AddSolutionAccessibility(uid);
            ChangeInjectorState(uid, true);
        }

        else
        {
            RemoveSolutionAccessibility(uid);
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

        var examine = EnsureComp<ExaminableSolutionComponent>(uid);
        examine.Solution = SolutionName;

        var draw = EnsureComp<DrawableSolutionComponent>(uid);
        draw.Solution = SolutionName;
    }

    private void RemoveSolutionAccessibility(EntityUid uid)
    {
        RemComp<RefillableSolutionComponent>(uid);
        RemComp<ExaminableSolutionComponent>(uid);
        RemComp<DrawableSolutionComponent>(uid);
    }
}
