using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Implants;

namespace Content.Shared._Impstation.Implants;

public sealed class AddMarkingImplantSystem: EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddMarkingImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    /// <summary>
    /// Add the markings to the recipient of the implant.
    /// </summary>
    private void OnImplantImplanted(Entity<AddMarkingImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        TryComp<HumanoidAppearanceComponent>(args.Implanted, out var appearanceComponent);

        var hairColor = Color.Black;

        // this is evil and really bad and im sorry. but nubody's going to fuck this up anyway so whatever
        if (ent.Comp.UseHairColor && appearanceComponent != null)
        {
            foreach (var hairMarking in appearanceComponent.MarkingSet.Markings[MarkingCategories.Hair])
            {
                hairColor = hairMarking.MarkingColors[0];
                break;
            }
        }

        foreach (var marking in ent.Comp.Markings)
        {
            _appearance.AddMarking(args.Implanted, marking, hairColor, forced: true);
        }
    }
}
