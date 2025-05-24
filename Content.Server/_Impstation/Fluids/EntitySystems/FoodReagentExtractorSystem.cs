using System.Linq;
using Content.Shared._Impstation.Fluids.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;

public sealed class FoodReagentExtractorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FoodReagentExtractorComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FoodReagentExtractorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var targetSolution = ent.Comp.SolutionName;
        if (targetSolution.AvailableVolume > 0)
            return;

        if (!TryComp<FoodComponent>(args.Used, out var food))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, food.Solution, out var solution))
            return;

        var reagentAmount = solution.Value.Comp.Solution.Contents
            .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());
        _solutionContainerSystem.TryAddSolution(ent.Comp.SolutionName, ent.Comp.ExtractedReagent, (int)reagentAmount);

        _audio.PlayPvs(ent.Comp.ExtractSound, ent);
        QueueDel(args.Used);
        args.Handled = true;
    }
}
