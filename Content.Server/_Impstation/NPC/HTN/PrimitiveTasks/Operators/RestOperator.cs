using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Impstation.NPC.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Handles and animals resting phase after an action
/// </summary>
public sealed partial class RestOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
    }

    /// <summary>
    /// Check if the Animal is Tired at which point set it to be resting
    /// </summary>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
    CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<AnimalNPCComponent>(owner, out var animalBrain))
            return (false, null);

        if (animalBrain.CurrentMood == AnimalMood.Tired)
        {
            animalBrain.CurrentMood = AnimalMood.Resting;

            var nextSleep = _random.Next(animalBrain.MinRestTime, animalBrain.MaxRestTime);
            animalBrain.EndRest = _time.CurTime + TimeSpan.FromSeconds(nextSleep);
        }

        return (true, null);
    }

    /// <summary>
    /// Handles our animals resting phase
    /// If the Animal Components Ending rest time has not passed, we keep the task going until the current time passes it
    /// at which point the animal goes back to being bored
    /// </summary>
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<AnimalNPCComponent>(owner, out var animalComp))
        {
            return HTNOperatorStatus.Failed;
        }

        if (animalComp.CurrentMood != AnimalMood.Resting)
        {
            return HTNOperatorStatus.Failed;
        }

        if (animalComp.EndRest > _time.CurTime)
        {
            return HTNOperatorStatus.Continuing;
        }

        animalComp.CurrentMood = AnimalMood.Bored;
        return HTNOperatorStatus.Finished;
    }
}
