using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Impstation.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;
public sealed partial class BreedablePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField("minHungerState", required: true)]
    public HungerThreshold MinHungerToBreed = HungerThreshold.Okay;

    [DataField("minThirstState", required: true)]
    public ThirstThreshold MinThirstToBreed = ThirstThreshold.Okay;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entityManager))
            return false;

        // We gotta be sure they're even capable of this
        if (!_entityManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp))
            return false;

        // Mobs should only search for a breedable mate if they are ready to breed
        // NextSearch exists so that mobs do not spam the server with searches and pathfinding to look for an eligible mate
        if (reproComp.NextBreed > _time.CurTime || reproComp.NextSearch > _time.CurTime)
            return false;

        reproComp.NextSearch = _time.CurTime + _random.Next(reproComp.MinSearchAttemptInterval, reproComp.MaxSearchAttemptInterval);

        return true;
        //    _entityManager.TryGetComponent<HungerComponent>(owner, out var hunger) ? hunger.CurrentThreshold >= MinHungerToBreed : false &&
        //    _entityManager.TryGetComponent<ThirstComponent>(owner, out var thirst) ? thirst.CurrentThirstThreshold >= MinThirstToBreed : false;
    }
}
