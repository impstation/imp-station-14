using Content.Server._CD.Body.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server._CD.Body.Systems;

public sealed class AllergySystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<AllergyComponent, ReactionEntityEvent>(OnReaction);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var allergies = args.Profile.CDAllergies;
        if (allergies is { Count: > 0 })
            AddComp(args.Mob, new AllergyComponent { Reagents = allergies });
    }

    private void OnReaction(EntityUid uid, AllergyComponent allergy, ref ReactionEntityEvent args)
    {
        if (!allergy.Reagents.TryGetValue(args.Reagent.ID, out var reaction))
            return;
        if (!TryComp(uid, out BloodstreamComponent? bloodstream))
            return;
        if (!_solutionContainerSystem.ResolveSolution(uid,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var solution))
            return;
        var quantity = args.ReagentQuantity.Quantity;
        solution.AddReagent(allergy.ReactionReagent, reaction * quantity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AllergyComponent, MetabolizerComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var allergy, out var metabolizer, out var bloodstream))
        {
            if (_gameTiming.CurTime < allergy.NextUpdate)
                continue;

            allergy.NextUpdate += metabolizer.UpdateInterval;

            if (allergy.Reagents.Count == 0)
                continue;

            if (!_solutionContainerSystem.ResolveSolution(uid,
                    bloodstream.ChemicalSolutionName,
                    ref bloodstream.ChemicalSolution,
                    out var chemstream))
                continue;

            var histamine = GetReaction(allergy, chemstream);
            foreach (var lung in _bodySystem.GetBodyOrganEntityComps<LungComponent>(uid))
            {
                if (lung.Comp1.Solution != null)
                    histamine += GetReaction(allergy, lung.Comp1.Solution!.Value.Comp.Solution);
            }
            chemstream.AddReagent(allergy.ReactionReagent, histamine);
        }
    }

    private FixedPoint2 GetReaction(AllergyComponent allergy, Solution solution)
    {
        var reaction = new FixedPoint2();
        foreach (var reagent in solution.Contents)
        {
            reaction += allergy.Reagents.GetValueOrDefault(reagent.Reagent.Prototype) * reagent.Quantity;
        }
        return reaction;
    }
}
