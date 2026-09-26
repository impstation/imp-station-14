using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Impstation.AnimalHusbandry.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// For checking if a mob is ready to begin breeding
/// </summary>
public sealed partial class BreedOperator : HTNOperator
{
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

    /// <summary>
    /// We don't need to update so we just finish. This is seen in a few other HTN operators as well.
    /// </summary>
    /// <param name="blackboard"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }

    #region OVERRIDES
    // These exist to make the blackboard happy. Do not think about them.
    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
    }

    public void Shutdown(NPCBlackboard blackboard)
    {
        blackboard.Remove<EntityUid>(Target);
    }

    /// <summary>
    /// Called when our task is done
    /// </summary>
    /// <param name="blackboard"></param>
    /// <param name="status"></param>
    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
    }

    /// <summary>
    /// Called when our entire plan is done
    /// </summary>
    /// <param name="blackboard"></param>
    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);
        Shutdown(blackboard);
    }
    #endregion
}
