using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Impstation.Nutrition.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Impstation.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;

/// <summary>
/// Precondition for determining if the breeder is able to be bred
/// Factors include: Hunger, Thirst and health.
/// </summary>
public sealed partial class BreedablePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private AnimalHusbandrySystemImp _breedSystem = default!;

    [DataField("minHungerState", required: true)]
    public HungerThreshold MinHungerToBreed = HungerThreshold.Okay;

    [DataField("minThirstState", required: true)]
    public ThirstThreshold MinThirstToBreed = ThirstThreshold.Okay;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _breedSystem = sysManager.GetEntitySystem<AnimalHusbandrySystemImp>();
    }

    /// <summary>
    /// The function for the HTN to check if a mob is ready to breed.
    /// </summary>
    /// <param name="blackboard"></param>
    /// <returns></returns>
    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entityManager))
            return false;

        // We gotta be sure they're even capable of this
        if (!_entityManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp))
            return false;

        // Mobs should only search for a breedable mate if they are ready to breed
        // NextSearch exists so that mobs do not spam the server with searches and pathfinding to look for an eligible mate
        if (!_breedSystem.CanIBreed((owner, reproComp)))
            return false;

        // Sets the amount of time until they try and find a new mate.
        // It's partly so all mobs aren't just in sync, but also so the server isn't spamming searches.
        reproComp.NextSearch = _time.CurTime + _random.Next(reproComp.MinSearchAttemptInterval, reproComp.MaxSearchAttemptInterval);

        return true;
    }
}
