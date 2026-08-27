using Content.Server._Impstation.AnimalHusbandry.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Robust.Shared.Map;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;

/// <summary>
///     Is the specified key capable of breeding?
/// </summary>
public sealed partial class TargetBreedablePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private AnimalHusbandrySystemImp _breedSystem = default!;

    [DataField("targetKey", required: true)] public string TargetKey = default!;

    public string RangeKey = default!;
    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _breedSystem = sysManager.GetEntitySystem<AnimalHusbandrySystemImp>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        // Make sure the target is valid and reproductive
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            !_entManager.TryGetComponent<ImpReproductiveComponent>(target, out var reproComp))
            return false;

        return _breedSystem.CanYouBreed((target, reproComp));
    }
}
