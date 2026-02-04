using Content.Server.Atmos.EntitySystems;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Genes.Components;
using Content.Shared.Actions;
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

        SubscribeLocalEvent<FireballGeneComponent, ActionPerformedEvent>(OnDoAction);
    }

    public override void OnGeneAdded(Entity<BaseGeneComponent> entity)
    {
        base.OnGeneAdded(entity);

        if (!_entityManager.TryGetComponent<FireballGeneComponent>(entity, out var component))
            return;

        EntityUid? actionId = null;
        _actionsSystem.AddAction(entity, ref actionId, component._action);
    }

    public void OnDoAction(Entity<FireballGeneComponent> entity, ref ActionPerformedEvent args)
    {
        if (!entity.Comp._activeChromosomes[Chromosome.Synchronizer])
        {
            _flammableSystem.AdjustFireStacks(entity, 100, null, true);
        }
    }
}
