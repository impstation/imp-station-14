using Content.Server.Humanoid;
using Content.Server._Impstation.Homunculi.Incubator;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._Impstation.Homunculi;

public sealed class HomunculusSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IncubatorSystem _incubator = default!;

    public bool CreateHomunculiWithDna(Entity<IncubatorComponent?> ent, Entity<SolutionComponent> solution, MapCoordinates mapCoordinates, [NotNullWhen(true)] out EntityUid? homunculus)
    {
        // If there's no DNA data in the solution, return
        if (!_incubator.HasDnaData(solution))
        {
            homunculus = null;
            return false;
        }

        // Save a copy of the solutions reagents, can't just use it straight up
        var reagentList = solution.Comp.Solution.Contents.ToList();
        List<string?> storedDna = [];
        List<Entity<HomunculusTypeComponent>> entities = [];

        foreach (var dnaList in reagentList.Select(reagent => reagent.Reagent.EnsureReagentData().OfType<DnaData>()))
        {
            storedDna.AddRange(dnaList.Select(dna => dna.DNA));
        }

        var query = EntityQueryEnumerator<HomunculusTypeComponent, DnaComponent>();
        while (query.MoveNext(out var entityUid,out var homunculusType, out var dna))
        {
            if (!VerifyAndUseRecipe(homunculusType, solution, reagentList))
                continue;

            if (storedDna.Contains(dna.DNA))
                entities.Add((entityUid, homunculusType));
        }
        if (entities.Count > 0)
        {
            CreateHomunculiFromEntities(entities, storedDna, mapCoordinates, out var realHomunculi);
            homunculus = realHomunculi;
            return true;
        }

        homunculus = null;
        return false;
    }

    public void CreateHomunculiFromEntities(List<Entity<HomunculusTypeComponent>> entities,List<string?> dnaData, MapCoordinates mapCoordinates, out EntityUid homunculus)
    {
        homunculus = EntityManager.Spawn(entities[0].Comp.HomunculusType, mapCoordinates);
        _transform.AttachToGridOrMap(homunculus);

        EnsureComp<DnaComponent>(homunculus, out var homunculiDnaComponent);

        homunculiDnaComponent.DNA = string.Join("", dnaData);

        SetHomunculusAppearance(entities,homunculus);
    }

    public bool VerifyAndUseRecipe(HomunculusTypeComponent homunculusComp, Entity<SolutionComponent> solution, List<ReagentQuantity> reagents)
    {
        if (!SatisfiesRecipe(homunculusComp, reagents))
            return false;

        var savedSolutions = solution.Comp.Solution.Contents.ToList();
        // Go through all the reagents in the saved solution, if the reagent matches one in the recipe, remove it
        // I have to check for reagent data because it needs to be specific or else it won't drain
        foreach (var (reagent, amount) in homunculusComp.Recipe)
        {
            var match = savedSolutions.FirstOrDefault(rq => rq.Reagent.Prototype == reagent);

            if (match.Reagent.Data != null)
                _solution.RemoveReagent(solution, reagent, amount, match.Reagent.Data);
            else
                _solution.RemoveReagent(solution, reagent, amount);
        }
        return true;
    }

    private static bool SatisfiesRecipe(HomunculusTypeComponent component, List<ReagentQuantity> reagents)
    {
        foreach (var required in component.Recipe)
        {
            var available = reagents.FirstOrDefault(r => r.Reagent.Prototype == required.Key);

            if (available.Quantity < required.Value)
                return false;
        }
        return true;
    }

    private void SetHomunculusAppearance(List<Entity<HomunculusTypeComponent>> entities, EntityUid homunculi)
    {
        var markingCategories = new List<MarkingCategories>
        {
            MarkingCategories.Head,
            MarkingCategories.Eyes,
            MarkingCategories.Snout,
            MarkingCategories.HeadSide,
            MarkingCategories.HeadTop,
        };
        List<Color> skinColors = [];
        List<Color> eyeColors = [];

        foreach (var urist in entities)
        {
            if (!TryComp<HumanoidAppearanceComponent>(urist, out var appearanceComponent))
                return;

            skinColors.Add(appearanceComponent.SkinColor);
            eyeColors.Add(appearanceComponent.EyeColor);
            if (urist == entities.First())
            {
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
        }
        if (!TryComp<HumanoidAppearanceComponent>(homunculi, out var homAppearanceComponent))
            return;

        if (skinColors.Count > 0)
            homAppearanceComponent.SkinColor =  BlendColors(skinColors);
        if (skinColors.Count > 0)
            homAppearanceComponent.EyeColor = BlendColors(eyeColors);
    }

    private static Color BlendColors(List<Color> colors)
    {
        var baseColor = Color.Black;

        foreach (var color in colors)
        {
            baseColor.R =+ color.R;
            baseColor.G =+ color.G;
            baseColor.B =+ color.B;
        }

        baseColor.R /= colors.Count;
        baseColor.G /= colors.Count;
        baseColor.B /= colors.Count;

        return baseColor;
    }
}
