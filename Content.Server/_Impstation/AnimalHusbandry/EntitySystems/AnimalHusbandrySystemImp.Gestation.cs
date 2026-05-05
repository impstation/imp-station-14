using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

public sealed partial class AnimalHusbandrySystemImp
{
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

        var offspring = SpawnOnTop(entity, entity.Comp.EntityToSpawn);

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
