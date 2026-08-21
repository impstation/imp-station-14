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

/// <summary>
/// Basically the Wizards Fireball attack but as a Gene
/// We borrow the actual Fireball action here but in the future changing that may be ideal.
/// </summary>
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

    /// <summary>
    /// Called when the Gene is added to an Entity.
    /// In this case we add the Component and the Fireball action to the Entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    public void OnGeneAdded(Entity<FireballGeneComponent> entity, ref GeneAddedEvent args)
    {
        if (!_entityManager.TryGetComponent<FireballGeneComponent>(entity, out var component))
            return;

        EntityUid? actionId = null;
        _actionsSystem.AddAction(entity, ref actionId, component._action);

        if(actionId != null) 
            entity.Comp._actionId = (EntityUid)actionId;
    }

    /// <summary>
    /// Called whenever the Action is used
    /// The purpose here is primarily for applying the Negative effect of the Gene
    /// If the Gene has the Synchronizer Chromosome, then the negative effect won't be applied.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    /// <remark>
    /// TODO: Solve this fucking thing. I had to crowbar a new event into the Action System for this to work.
    /// Fun fact: Actions don't recognise who used said action, at least not that i could find.
    /// The normal action event entity is the fucking ACTION BUTTON ITSELF 
    /// </remark>
    public void OnDoAction(Entity<ActionComponent> entity, ref ActionPerformedEvent args)
    {
        if (!_entityManager.TryGetComponent<FireballGeneComponent>(args.Performer, out var fireComp))
            return;

        if (fireComp._actionId != entity.Comp.Owner)
            return;

        if (!_entityManager.TryGetComponent<GeneHostComponent>(args.Performer, out var geneHostComp))
            return;

        if(fireComp._negativeEffect != null && !fireComp._activeChromosomes[Chromosome.Synchronizer])
            fireComp._negativeEffect.ApplyGeneEffect((args.Performer, geneHostComp), fireComp._activeChromosomes);
    }
}
