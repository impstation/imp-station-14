using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.Construction.Steps;

[DataDefinition]
public sealed partial class EntityRemoveConstructionGraphStep : ConstructionGraphStep
{
    /// <summary>
    /// A tag of the item you want to remove.
    /// </summary>
    [DataField("remove")]
    public string Tag = string.Empty;

    /// <summary>
    /// A string representing the '$name' variable of the Loc file. By default, it's "Next, remove {$name}".
    /// </summary>
    [DataField]
    public string Name { get; private set; } = "something";

    /// <summary>
    /// A localization string used when examining and for the guidebook.
    /// </summary>
    [DataField]
    public LocId GuideString = "construction-remove-arbitrary-entity";

    public bool EntityValid(EntityUid uid, BaseContainer container, IEntityManager entityManager)
    {
        return entityManager.System<RemoveEntitySysten>().IsValid(uid, container, Tag);
    }

    public override void DoExamine(ExaminedEvent args)
    {
        if (string.IsNullOrEmpty(Name))
            return;

        var name = Loc.GetString(Name);
        args.PushMarkup(Loc.GetString(GuideString, ("name", name)));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        var name = Loc.GetString(Name);
        return new ConstructionGuideEntry
        {
            Localization = "arbitrary-remove-construction-graph-step",
            Arguments = new (string, object)[] { ("name", name) },
        };
    }
}
