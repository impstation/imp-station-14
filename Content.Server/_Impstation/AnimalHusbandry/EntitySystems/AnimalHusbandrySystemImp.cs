using Content.Server.Administration.Logs;
using Content.Server.Cloning;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared._Impstation.AnimalHusbandry.Components;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
/// System that handles mob breeding, birthing and growing
/// This system works alongside HTN
/// </summary>
public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    private TimeSpan _lastUpdated = TimeSpan.Zero;
    private TimeSpan _updateRate = TimeSpan.FromSeconds(1.0f); // TODO: CVar

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, IsUnableToGestateEvent>(IsMobStateUnableToGestate);
        SubscribeLocalEvent<MindContainerComponent, IsUnableToGestateEvent>(IsMindContainerUnableToGestate);
    }

    /// <summary>
    ///     Advance the gestation of all pregnant entities and the growth of all infant entities.
    ///     This happens on an interval loop for performance reasons.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We only update pregnancy and growth timers every [interval], for performance.
        if (_time.CurTime < _lastUpdated + _updateRate)
            return;

        // Time elapsed since last update. May be slightly more than [interval], depending on frame time.
        var growthTime = _time.CurTime - _lastUpdated;
        _lastUpdated = _time.CurTime;

        // Advance the gestation of all gestating entities.
        var gestatingQuery = EntityQueryEnumerator<GestatingComponent>();
        while (gestatingQuery.MoveNext(out var uid, out var gestating))
        {
            var gestatingEntity = (uid, gestating);

            // If the entity is unable to gestate for any reason,
            // then we end the gestation pre-emptively without producing anything.
            if (IsUnableToGestate(gestatingEntity))
            {
                EndGestation(gestatingEntity);
                continue;
            }

            // Otherwise, if this entity is capable of gestation, then we add progress to the gestation timer.
            if (IsGestating(gestatingEntity))
                gestating.CurrentGestationTime += growthTime;

            // If the gestation progress timer surpasses the gestation time, complete gestation.
            if (gestating.CurrentGestationTime > gestating.GestationTime)
                CompleteGestation(gestatingEntity);
        }

        // Advance the growth of all infants.
        var infantQuery = EntityQueryEnumerator<ImpInfantComponent>();
        while (infantQuery.MoveNext(out var uid, out var infant))
        {
            // If the infant isn't alive (crit or dead), then it shouldn't be growing.
            if (!IsAlive(uid))
                continue;

            infant.CurrentGrowthTime += growthTime;

            // Advance if the infant is done growing.
            if (infant.CurrentGrowthTime >= infant.GrowthTime)
                AdvanceStage((uid, infant));
        }
    }

    /// <summary>
    /// Handles growing an infant by deleting the current mob and making a new one
    /// This also transfers the previous components to the new mob
    /// </summary>
    /// <param name="infant">The infant that will be growing up</param>
    /// <returns></returns>
    private bool AdvanceStage(Entity<ImpInfantComponent> infant)
    {
        bool isAdult = false;

        var newStage = SpawnNewMob(infant, infant.Comp.NextStage);

        if (newStage == null)
            return false;

        if (!_prototype.Resolve(infant.Comp.OffspringSettings, out var settings))
            return false;

        // If someone is in this thing, move them over as well
        if (_mind.TryGetMind(infant, out var mind, out var mindComp))
            _mind.TransferTo(mind, newStage);

        isAdult = !_entManager.TryGetComponent<InfantComponent>(newStage, out var comp);

        // Make sure the relevant Data like damage carries over
        _cloning.CloneComponents(infant, (EntityUid)newStage, settings);

        // If there is a ghost role attached to this mob, keep it
        // We also make sure the new stage doesn't already come with one, otherwise the game crashes.
        if (_entManager.TryGetComponent<GhostRoleComponent>(infant, out var ghostComp) &&
            !_entManager.TryGetComponent<GhostRoleComponent>(newStage, out var newGhostcomp))
        {
            CopyComp(infant, (EntityUid)newStage, ghostComp);
        }

        // Pompeii Ash Baby.png
        QueueDel(infant);

        // So they don't immediately try to breed the second they grow up
        if (isAdult && _entManager.TryGetComponent<ImpReproductiveComponent>(newStage, out var reproComp))
            reproComp.NextSearch = _time.CurTime + _random.Next(reproComp.MinSearchAttemptInterval, reproComp.MaxSearchAttemptInterval);

        return isAdult;
    }

    /// <summary>
    /// Handles spawning a new mob for us
    /// </summary>
    /// <param name="entity">Entity calling the creation</param>
    /// <param name="toSpawn">What entity will be spawned</param>
    /// <returns>The ID of our new mob! Or nothing if for some reason it didn't spawn.</returns>
    public EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);
        var newMob = Spawn(toSpawn, xform.Coordinates);

        return newMob;
    }

    /// <summary>
    ///     Checks whether or not an entity is alive.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>Whether or not the entity is alive or otherwise does not require health.</returns>
    public bool IsAlive(Entity<MobStateComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return true;

        return entity.Comp.CurrentState == Shared.Mobs.MobState.Alive;
    }
}
