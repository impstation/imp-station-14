using Content.Server._Impstation.Genetics.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Impstation.Genetics.Events;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Genes.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Genes.EntitySystems;

public sealed partial class FireballGeneSystem : BaseGeneEntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireballGeneComponent, GeneAddedEvent>(OnGeneAdded);
        SubscribeLocalEvent<ActionComponent, ActionPerformedEvent>(OnDoAction);
    }

    public void OnGeneAdded(Entity<FireballGeneComponent> entity, ref GeneAddedEvent args)
    {
        if (!_entityManager.TryGetComponent<FireballGeneComponent>(entity, out var component))
            return;

        EntityUid? actionId = null;
        _actionsSystem.AddAction(entity, ref actionId, component._action);

        if(actionId != null) 
            entity.Comp._actionId = (EntityUid)actionId;
    }

    public void OnDoAction(Entity<ActionComponent> entity, ref ActionPerformedEvent args)
    {
        //if (args.Performer.Id == entity.Comp._action.)
        //    return;

        if (!_entityManager.TryGetComponent<FireballGeneComponent>(args.Performer, out var fireComp))
            return;

        if (fireComp._actionId != entity.Comp.Owner)
            return;

        if (!_entityManager.TryGetComponent<GeneHostComponent>(args.Performer, out var geneHostComp))
            return;

        if(fireComp._negativeEffect != null && !fireComp._activeChromosomes[Chromosome.Synchronizer])
            fireComp._negativeEffect.ApplyGeneEffect((args.Performer, geneHostComp), fireComp._activeChromosomes);

        /*
        if(_actionsSystem.)

        if (!_entityManager.TryGetComponent<GeneHostComponent>(entity, out var component))
            return;

        if (entity.Comp._negativeEffect != null && !entity.Comp._activeChromosomes[Chromosome.Synchronizer])
        {
            entity.Comp._negativeEffect.ApplyGeneEffect((entity.Owner, component), entity.Comp._activeChromosomes);
        }*/
    }
}
