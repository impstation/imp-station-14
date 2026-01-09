using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Impstation.Nutrition.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;
/// <summary>
/// Precondition for determining if the animal approached is able to be bred
/// Factors include: Hunger, Thirst, Distance and health.
/// </summary>
public sealed partial class BreedReadyPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entityManager;

    [DataField("targetKey")]
    public string TargetKey = "Target";

    [DataField("breedRange")]
    public string BreedRange = "MeleeRange";

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entityManager))
            return false;

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entityManager))
            return false;

        if (!_entityManager.TryGetComponent<ImpReproductiveComponent>(target, out var targetRepro))
            return false;

        if (!_entityManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp))
            return false;

        if (!_entityManager.TryGetComponent<TransformComponent>(owner, out var trans))
            return false;

        if (!_entityManager.TryGetComponent<TransformComponent>(target, out var targetTrans))
            return false;

        var range = blackboard.GetValueOrDefault<float>(BreedRange, _entityManager);

        if (!trans.Coordinates.TryDistance(_entityManager, targetTrans.Coordinates, out var distance) || distance > range)
            return false;

        reproComp.PartnerInMind = false;

        if (targetRepro.Pregnant)
            return false;

        return true;
    }
}
