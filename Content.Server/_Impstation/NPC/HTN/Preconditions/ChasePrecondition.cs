using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Impstation.NPC.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;

/// <summary>
/// Handles the Conditions for an animal to chase something based on its mood
/// Also ends the chase based on a timer, at which point the whole HTN branch will cancel.
/// </summary>
public sealed partial class ChasePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        CanFailLater = true;
        base.Initialize(sysManager);
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // We either have no animal brain OR we aren't ready for the next chase
        // OR If we aren't bored or already chasing
        if (!_entManager.TryGetComponent<AnimalNPCComponent>(owner, out var animalBrain) ||
            (animalBrain.CurrentMood != AnimalMood.Bored && animalBrain.CurrentMood != AnimalMood.Chasing))
            return false;

        // Is it time to end the chase?
        if (animalBrain.CurrentMood == AnimalMood.Chasing && animalBrain.EndChase < _time.CurTime)
        {
            animalBrain.CurrentMood = AnimalMood.Tired;
            return false;
        }

        return true;
    }
}
