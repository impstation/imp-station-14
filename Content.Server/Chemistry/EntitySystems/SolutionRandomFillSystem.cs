using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq; // imp
using Content.Shared.FixedPoint; // imp

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionRandomFillSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomFillSolutionComponent, MapInitEvent>(OnRandomSolutionFillMapInit);
    }

    private void OnRandomSolutionFillMapInit(Entity<RandomFillSolutionComponent> entity, ref MapInitEvent args)
    {
        // imp edit start
        string reagent;
        FixedPoint2 quantity;

        if (entity.Comp.TrueRandomMode)
        {
            var allReagents = _proto.EnumeratePrototypes<ReagentPrototype>()
                .Where(x => !x.Abstract)
                .Select(x => new ProtoId<ReagentPrototype>(x.ID))
                .ToList();

            allReagents.RemoveAll(r => entity.Comp.BlacklistedReagents.Contains(r));

            allReagents.RemoveAll(r => entity.Comp.BlacklistedGroups.Any(a => a == _proto.Index(r).Group) && !entity.Comp.WhitelistedReagents.Contains(r));

            reagent = _random.Pick(allReagents);
            quantity = _random.Next(entity.Comp.RandomAmountMin / entity.Comp.RandomAmountStep, entity.Comp.RandomAmountMax / entity.Comp.RandomAmountStep) * entity.Comp.RandomAmountStep;
        }
        else
        {
            if (entity.Comp.WeightedRandomId == null)
                return;

            var pick = _proto.Index<WeightedRandomFillSolutionPrototype>(entity.Comp.WeightedRandomId).Pick(_random);

            reagent = pick.reagent;
            quantity = pick.quantity;
        }
        // imp edit end

        /* imp edit, we move it up into the else
        if (entity.Comp.WeightedRandomId == null)
            return;

        var pick = _proto.Index<WeightedRandomFillSolutionPrototype>(entity.Comp.WeightedRandomId).Pick(_random);

        var reagent = pick.reagent;
        var quantity = pick.quantity;
        */

        if (!_proto.HasIndex<ReagentPrototype>(reagent))
        {
            Log.Error($"Tried to add invalid reagent Id {reagent} using SolutionRandomFill.");
            return;
        }

        _solutionsSystem.EnsureSolutionEntity(entity.Owner, entity.Comp.Solution, out var target , quantity); // imp edit, pick.quantity -> quantity
        if(target.HasValue)
            _solutionsSystem.TryAddReagent(target.Value, reagent, quantity);
    }
}
