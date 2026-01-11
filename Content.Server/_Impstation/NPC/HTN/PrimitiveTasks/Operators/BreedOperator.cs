using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Impstation.Nutrition.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// For checking if a mob is ready to begin breeding
/// </summary>
public sealed partial class BreedOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private AnimalHusbandrySystemImp _breedSystem = default!;

    [DataField("targetKey")]
    public string Target = "Target";
    [DataField("idleKey")]
    public string IdleKey = "IdleTime";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _breedSystem = sysManager.GetEntitySystem<AnimalHusbandrySystemImp>();
    }

    /// <summary>
    /// Task for making a mob attempt to breed with the target
    /// </summary>
    /// <param name="blackboard"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(Target);
        _breedSystem.TryBreedWithTarget(owner, target);

        return new(true, new Dictionary<string, object>()
        {
            { IdleKey, 1f }
        });
    }

    // I don't know why this is here yet and if i can't figure it out i'll be removing it
    // yes it's my code but i don't know which era of me wrote it
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var result = false;



        return result ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
