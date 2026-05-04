using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Server._Impstation.AnimalHusbandry.EntitySystems;
using Content.Shared._Impstation.AnimalHusbandry.Components;

namespace Content.Server._Impstation.NPC.HTN.Preconditions;

/// <summary>
/// Precondition for determining if the breeder is able to be bred
/// Factors include: Hunger, Thirst and health.
/// </summary>
public sealed partial class BreedablePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private AnimalHusbandrySystemImp _breedSystem = default!;

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

        var reproducer = (owner, reproComp);

        // Mobs should only search for a breedable mate if they are ready to breed
        if (!_breedSystem.ReadyToSearch(reproducer) || !_breedSystem.CanYouBreed(reproducer))
            return false;

        // Sets the amount of time until they try and find a new mate.
        // It's partly so all mobs aren't just in sync, but also so the server isn't spamming searches.
        _breedSystem.RefreshSearchTime(reproducer);

        return true;
    }
}
