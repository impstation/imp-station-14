using Content.Shared._Impstation.Genetics.Events;
using Content.Shared._Impstation.Genetics.Genes.Components;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.Genetics.Genes.EntitySystems;

public sealed partial class IonizeGeneSystem : EntitySystem
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

        if (!TryComp<PointLightComponent>(entity.Owner, out var pointlight))
            pointlight = AddComp<PointLightComponent>(entity.Owner);
        else
        {
            entity.Comp.Previous = new LightProfile
            (
                pointlight.Energy,
                pointlight.Color,
                pointlight.Radius
            );
        }

        if (!TryComp<RadiationSourceComponent>(entity.Owner, out var rads))
            rads = AddComp<RadiationSourceComponent>(entity.Owner);

        _lightSystem.SetEnergy(entity.Owner, entity.Comp.Light.LightEnergy);
        _lightSystem.SetColor(entity.Owner, entity.Comp.Light.LightColor);
        _lightSystem.SetRadius(entity.Owner, entity.Comp.Light.Radius);

        rads.Intensity = entity.Comp.RadStrength;
    }

    public void OnGeneRemoved(Entity<IonizeGeneComponent> entity, ref GeneRemovedEvent args)
    {

    }
}
