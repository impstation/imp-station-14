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
public sealed partial class ChasePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // We either have no animal brain OR we aren't ready for the next chase
        // OR If we aren't bored or already chasing
        if (!_entManager.TryGetComponent<AnimalComponent>(owner, out var animalBrain) ||
            animalBrain.NextChase > _time.CurTime ||
            (animalBrain.CurrentMood != AnimalMood.Bored && animalBrain.CurrentMood != AnimalMood.Chasing))
            return false;

        // Is it time to end the chase?
        if (animalBrain.CurrentMood == AnimalMood.Chasing && animalBrain.EndChase < _time.CurTime)
        {
            animalBrain.CurrentMood = AnimalMood.Tired;
            animalBrain.NextChase = _time.CurTime + TimeSpan.FromSeconds(animalBrain.MinRestTime, animalBrain.MaxRestTime);

            //var htn = _entManager.GetComponent<HTNComponent>(owner);

            //_htnSystem.ShutdownPlan(htn);

            return false;
        }

        return true;
    }
}
