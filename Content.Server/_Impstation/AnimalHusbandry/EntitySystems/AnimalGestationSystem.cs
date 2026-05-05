using Content.Server.Administration.Logs;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
///     This system handles the gestation of offspring for breedable animals, such as animal pregnancy and eggs.
/// </summary>
public sealed partial class AnimalGestationSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AnimalHusbandrySystemImp _animalHusbandry = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IGameTiming _time = default!;

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
    }

    /// <summary>
    ///     Advance the gestation of all pregnant entities.
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

        // Clean up entities that need to be deleted
        foreach (var uid in toDelete)
            QueueDel(uid);
    }

    /// <summary>
    ///     Gets whether or not this entity is currently gestating.
    /// </summary>
    /// <remarks>
    ///     This can be prevented by certain circumstances, like an entity that requires an incubator to gestate.
    /// </remarks>
    /// <param name="ent">The gestating entity.</param>
    /// <returns>Whether or not this entity is currently gestating.</returns>
    public bool IsGestating(Entity<GestatingComponent?> ent)
    {
        // Not a gestating entity.
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // Being deleted.
        if (TerminatingOrDeleted(ent))
            return false;

        var ev = new IsGestatingEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        return !ev.Cancelled;
    }

    /// <summary>
    ///     Gets whether or not this entity is currently incapable of gestation.
    /// </summary>
    /// <remarks>
    ///     The entity does not have to be already gestating to be checked, as this also
    ///     checks the reproductive viability of mobs that are about to breed.
    /// </remarks>
    /// <param name="ent">The entity to check gestation capability.</param>
    /// <returns>Whether or not this entity is currently incapable of gestation</returns>
    public bool IsUnableToGestate(EntityUid uid)
    {
        // Being deleted.
        if (TerminatingOrDeleted(uid))
            return false;

        var ev = new IsUnableToGestateEvent();
        RaiseLocalEvent(uid, ref ev);

        return ev.Handled;
    }

    /// <summary>
    ///     Renders a mob unable to gestate if it is in dead or critical condition.
    /// </summary>
    /// <param name="ent">The mob state entity.</param>
    private void IsMobStateUnableToGestate(Entity<MobStateComponent> ent, ref IsUnableToGestateEvent args)
    {
        if (ent.Comp.CurrentState != Shared.Mobs.MobState.Alive)
            args.Handled = true;
    }

    /// <summary>
    ///     Renders an entity unable to gestate if there is a player controlling it.
    /// </summary>
    /// <param name="ent">The mind container entity.</param>
    private void IsMindContainerUnableToGestate(Entity<MindContainerComponent> ent, ref IsUnableToGestateEvent args)
    {
        if (_mind.TryGetMind(ent.Owner, out var _, out var _, ent.Comp))
            args.Handled = true;
    }

    /// <summary>
    ///     Prevents infant entities from gestating at all.
    /// </summary>
    /// <param name="ent">The infant entity.</param>
    private void IsInfantUnableToGestate(Entity<ImpInfantComponent> ent, ref IsUnableToGestateEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    ///     Completes gestation and spawns a new entity, cleaning up if needed.
    /// </summary>
    /// <param name="entity">The gestating entity.</param>
    private void CompleteGestation(Entity<GestatingComponent> entity)
    {
        if (TryComp<InteractionPopupComponent>(entity, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, entity);

        var offspring = _animalHusbandry.SpawnOnTop(entity, entity.Comp.EntityToSpawn);

        if (TryComp<ImpInfantComponent>(offspring, out var infantComp))
            infantComp.Parent = entity;

        _adminLog.Add(LogType.Action,
            $"{ToPrettyString(entity)} gave birth to {ToPrettyString(offspring)}!"
            + $" DELETED: {entity.Comp.DeleteSelfOnSpawn}");

        EndGestation(entity.AsNullable());
    }

    /// <summary>
    ///     Stops an entity's gestation immediately.
    /// </summary>
    /// <remarks>
    ///     This does not produce offspring, but it is called in <see cref="CompleteGestation"/>.
    /// </remarks>
    /// <param name="entity">The gestating entity.</param>
    private void EndGestation(Entity<GestatingComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return;

        RemCompDeferred(entity.Owner, entity.Comp);
    }
}
