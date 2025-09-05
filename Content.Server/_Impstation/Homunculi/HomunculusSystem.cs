using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Humanoid;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Impstation.Homunculi;

public sealed class HomunculusSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public bool CreateHomunculiWithDna(Entity<IncubatorComponent?> ent, Entity<SolutionComponent> solution, MapCoordinates mapCoordinates, [NotNullWhen(true)] out EntityUid? homunculus)
    {
        // If there's no DNA data in the solution, return
        if (!TryGetDnaData(solution, out var dnaData))
        {
            homunculus = null;
            return false;
        }

        // Save a copy of the solutions reagents, can't just use it straight up
        var reagentList = solution.Comp.Solution.Contents.ToList();

        var query = EntityQueryEnumerator<HomunculiTypeComponent>();
        while (query.MoveNext(out var victim, out var homunculiTypeComponent))
        {
            if (!VerifyAndUseRecipe(homunculiTypeComponent, solution, reagentList))
                continue;

            CreateHomunculiOfEntity(victim, homunculiTypeComponent, mapCoordinates, out var realHomunculi);
            homunculus = realHomunculi;
            return true;
        }
        homunculus = null;
        return false;
    }

    public void CreateHomunculiOfEntity(EntityUid entity, HomunculiTypeComponent homunculiType, MapCoordinates mapCoordinates, out EntityUid homunculus)
    {
        homunculus = EntityManager.Spawn(homunculiType.HomunculiType, mapCoordinates);
        _transform.AttachToGridOrMap(homunculus);

        EnsureComp<DnaComponent>(homunculus, out var homunculiDnaComponent);

        if (TryComp<DnaComponent>(entity, out var dna))
            homunculiDnaComponent.DNA = dna.DNA;

        SetHomunculusAppearance(entity,homunculus);
    }

    public bool VerifyAndUseRecipe(HomunculiTypeComponent homunculiComp, Entity<SolutionComponent> solution, List<ReagentQuantity> reagents)
    {
        if (!SatisfiesRecipe(homunculiComp, reagents))
            return false;

        var savedSolutions = solution.Comp.Solution.Contents.ToList();
        // Go through all the reagents in the saved solution, if the reagent matches one in the recipe, remove it
        // I have to check for reagent data because it needs to be specific or else it won't drain
        foreach (var (reagent, amount) in homunculiComp.Recipe)
        {
            var match = savedSolutions.FirstOrDefault(rq => rq.Reagent.Prototype == reagent);

            if (match.Reagent.Data != null)
                _solution.RemoveReagent(solution, reagent, amount, match.Reagent.Data);
            else
                _solution.RemoveReagent(solution, reagent, amount);
        }
        return true;
    }

    private static bool SatisfiesRecipe(HomunculiTypeComponent component, List<ReagentQuantity> reagents)
    {
        foreach (var required in component.Recipe)
        {
            var available = reagents.FirstOrDefault(r => r.Reagent.Prototype == required.Key);

            if (available.Quantity < required.Value)
                return false;
        }
        return true;
    }

    private void SetHomunculusAppearance(EntityUid urist, EntityUid homunculi)
    {
        var markingCategories = new List<MarkingCategories>
        {
            MarkingCategories.Head,
            MarkingCategories.Eyes,
            MarkingCategories.Snout,
            MarkingCategories.HeadSide,
            MarkingCategories.HeadTop,
        };

        if (!TryComp<HumanoidAppearanceComponent>(urist, out var appearanceComponent))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(homunculi, out var homAppearanceComponent))
            return;

        homAppearanceComponent.SkinColor = appearanceComponent.SkinColor;
        homAppearanceComponent.EyeColor = appearanceComponent.EyeColor;

        foreach (var markingPair in appearanceComponent.MarkingSet.Markings)
        {
            if (!markingCategories.Contains(markingPair.Key))
                continue;

            foreach (var marking in markingPair.Value)
            {
                _appearance.AddMarking(homunculi, marking.MarkingId, marking.MarkingColors);
            }
        }
    }

    private static bool TryGetDnaData(SolutionComponent solution, [NotNullWhen(true)] out List<DnaData>? dnaData)
    {
        dnaData = [];
        dnaData.AddRange(solution.Solution.Contents.SelectMany(reagent
                => reagent.Reagent.EnsureReagentData())
            .OfType<DnaData>());
        return dnaData.Count > 0;
    }
}
