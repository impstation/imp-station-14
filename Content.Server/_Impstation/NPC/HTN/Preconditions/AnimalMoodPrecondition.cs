using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Impstation.NPC.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the Animal's mood is what we want for the Task list
/// TODO: Allow for multiple mood checks at once
/// </summary>
public sealed partial class AnimalMoodPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("animalMood")]
    public string AnimalMood = "none";

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<AnimalNPCComponent>(owner, out var animalBrain))
            return false;

        return animalBrain.CurrentMood.ToString() == AnimalMood;
    }
}
