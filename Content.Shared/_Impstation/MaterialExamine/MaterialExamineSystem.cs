using System.Text;
using Content.Shared.Examine;
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
        var text = new StringBuilder();

        var index = 0;
        foreach (var (id, amount) in component.MaterialComposition)
        {
            // this sucks and i hate it but i dont know any other way to do it <3
            if (index > 0 && component.MaterialComposition.Count > 2)
                text.Append(",");

            text.Append(" ");

            if (index == component.MaterialComposition.Count - 1 && component.MaterialComposition.Count > 1)
                text.Append("and ");

            if (!_prototypeManager.TryIndex<MaterialPrototype>(id, out var proto))
                continue;

            text.Append("[color=" + proto.Color.ToHex() + "]" + Loc.GetString(proto.Name) + "[/color]");
            index++;
        }

        return text.ToString();
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
}
