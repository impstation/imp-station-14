using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.MaterialExamine;

public sealed class MaterialExamineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalCompositionComponent, ExaminedEvent>(OnExamine);
    }

    private string ConstructMaterialList(PhysicalCompositionComponent component)
    {
        var materials = new List<string>();

        foreach (var keyValuePair in component.MaterialComposition)
        {
            if (!_prototypeManager.TryIndex<MaterialPrototype>(keyValuePair.Key, out var proto))
                continue;

            var recognisedColorHSL = Color.ToHsl(proto.Color);
            recognisedColorHSL.Z = RescaleLuminosity((float) recognisedColorHSL.Z);

            materials.Add(Loc.GetString("examinable-material",
                ("color", Color.FromHsl(recognisedColorHSL).ToHexNoAlpha()), // imp
                ("material", Loc.GetString(proto.Name))));
        }

        return ContentLocalizationManager.FormatList(materials);
    }

    private void OnExamine(EntityUid uid, PhysicalCompositionComponent component, ExaminedEvent args)
    {
        if (component.MaterialComposition.Count == 0)
            return;

        // you dont need to be told the plastic is made out of plastic
        if (HasComp<MaterialComponent>(uid))
            return;

        args.PushMarkup(Loc.GetString("material-examine", ("target", uid), ("materials", ConstructMaterialList(component))));
    }

    // thank you SharedSolutionContainerSystem for this <3
    private float RescaleLuminosity(float luminosity)
    {
        if (luminosity > 0.5){
            return luminosity;
        }
        return (float) ((luminosity * 0.2) + 0.4);
    }
}
