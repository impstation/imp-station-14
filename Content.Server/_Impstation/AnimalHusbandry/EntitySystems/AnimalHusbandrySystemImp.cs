using Content.Server.Administration.Logs;
using Content.Server.Cloning;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
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

        // Note: If you're implementing these events for other components, ideally they should be subscribed to in their
        // respective systems. These are here because they're upstream components and it would be a pain in the ass
        // to do so in a namespace-friendly way
        SubscribeLocalEvent<ImpInfantComponent, IsUnableToGestateEvent>(IsInfantUnableToGestate);
        SubscribeLocalEvent<MobStateComponent, IsUnableToGestateEvent>(IsMobStateUnableToGestate);
        SubscribeLocalEvent<MindContainerComponent, IsUnableToGestateEvent>(IsMindContainerUnableToGestate);

        SubscribeLocalEvent<MobStateComponent, InfantCanGrowEvent>(CanMobStateGrow);
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
        var toDelete = new List<EntityUid>();

        while (gestatingQuery.MoveNext(out var uid, out var gestating))
        {
            var gestatingEntity = (uid, gestating);

            // If the entity is unable to gestate for any reason,
            // then we end the gestation pre-emptively without producing anything.
            if (IsUnableToGestate(uid))
            {
                EndGestation(gestatingEntity);
                continue;
            }

            // Otherwise, if this entity is capable of gestation, then we add progress to the gestation timer.
            if (IsGestating(gestatingEntity))
                gestating.CurrentGestationTime += growthTime;

            // If the gestation progress timer surpasses the gestation time, complete gestation.
            if (gestating.CurrentGestationTime > gestating.GestationTime)
            {
                CompleteGestation(gestatingEntity);
                // Delete this entity on gestation complete if flagged for it - for example, an egg.
                if (gestating.DeleteSelfOnSpawn)
                    toDelete.Add(uid);
            }
        }

        // Advance the growth of all infants.
        var infantQuery = EntityQueryEnumerator<ImpInfantComponent>();
        while (infantQuery.MoveNext(out var uid, out var infant))
        {
            // Account for situations in which the infant is unable to grow.
            if (!CanGrow((uid, infant)))
                continue;

            infant.CurrentGrowthTime += growthTime;

            // Advance if the infant is done growing.
            if (infant.CurrentGrowthTime >= infant.GrowthTime)
                AdvanceStage((uid, infant));
        }

        // Clean up entities that need to be deleted
        foreach (var uid in toDelete)
            QueueDel(uid);
    }

    /// <summary>
    /// Handles spawning a new mob for us
    /// </summary>
    /// <param name="entity">Entity calling the creation</param>
    /// <param name="toSpawn">What entity will be spawned</param>
    /// <returns>The ID of our new mob! Or nothing if for some reason it didn't spawn.</returns>
    public EntityUid SpawnOnTop(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);
        var newMob = SpawnAtPosition(toSpawn, xform.Coordinates);

        return newMob;
    }
}
