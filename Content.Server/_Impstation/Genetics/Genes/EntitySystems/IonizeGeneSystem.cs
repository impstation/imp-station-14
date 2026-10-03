using Content.Shared._Impstation.Genetics.Events;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Genes.Components;
using Content.Shared._Impstation.Genetics.Genes.EntitySystems;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.Genetics.Genes.EntitySystems;

public sealed partial class IonizeGeneSystem : BaseGeneEntitySystem
{
    [Dependency] private readonly PointLightSystem _lightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IonizeGeneComponent, GeneAddedEvent>(OnGeneAdded);
        SubscribeLocalEvent<IonizeGeneComponent, GeneRemovedEvent>(OnGeneRemoved);
    }

    public void OnGeneAdded(Entity<IonizeGeneComponent> entity, ref GeneAddedEvent args)
    {
        if (args.Gene != entity.Comp.GeneName)
            return;

        if (!TryComp<RadiationSourceComponent>(entity.Owner, out var rads))
            rads = AddComp<RadiationSourceComponent>(entity.Owner);

        rads.Intensity = entity.Comp.RadStrength;

        base.ApplyEffects((entity.Owner, entity.Comp));
    }

    public void OnGeneRemoved(Entity<IonizeGeneComponent> entity, ref GeneRemovedEvent args)
    {

    }
}
