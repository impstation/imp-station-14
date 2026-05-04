using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
/// Handles the system for incubating eggs or whatever else you want incubatable.
/// </summary>
public sealed class IncubationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerSystem = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    bool _justSwapped = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggIncubatorComponent, ComponentStartup>(OnEggIncubatorStartup);
        SubscribeLocalEvent<EggIncubatorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<EggIncubatorComponent, EntInsertedIntoContainerMessage>(OnIncubatorContentsInserted);
        SubscribeLocalEvent<EggIncubatorComponent, EntRemovedFromContainerMessage>(OnIncubatorContentsRemoved);
    }

    /// <summary>
    /// Increases the incubation timer of all active incubating entities.
    /// </summary>
    /// <param name="frameTime">Time between frames</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IncubationComponent>();
        while (query.MoveNext(out var uid, out var incubation))
        {
            // We only increase the incubation timer every [interval], for performance.
            if (_time.CurTime < incubation.LastUpdateTime + incubation.UpdateRate)
                continue;

            // Time elapsed since last update. May be slightly more/less than [interval], depending on frame time.
            var incubationTime = _time.CurTime - incubation.LastUpdateTime;
            incubation.LastUpdateTime = _time.CurTime;

            // Move on if this entity is not being incubated.
            if (!IsBeingIncubated((uid, incubation)))
                continue;

            // Add incubation time to the incubator.
            incubation.CurrentIncubationTime += incubationTime;

            // Finish incubation if we're done incubating the egg.
            if (incubation.CurrentIncubationTime >= incubation.IncubationTime)
                FinishIncubation((uid, incubation));
        }
    }

    /// <summary>
    ///     Update the visuals of an egg incubator on startup.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnEggIncubatorStartup(Entity<EggIncubatorComponent> entity, ref ComponentStartup args)
    {
        UpdateIncubatorVisuals(entity);
    }

    /// <summary>
    ///     Update the visuals of an egg incubator if its power status changes,
    ///     and updates the "last updated time" of the eggs in the incubator.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnPowerChanged(Entity<EggIncubatorComponent> entity, ref PowerChangedEvent args)
    {
        UpdateIncubatorVisuals(entity);

        if (args.Powered)
            UpdateIncubatorContents(entity);
    }

    /// <summary>
    ///     Update the visuals of an egg incubator if an entity is added to its contents.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnIncubatorContentsInserted(Entity<EggIncubatorComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        UpdateIncubatorVisuals(entity);
    }

    /// <summary>
    ///     Update the visuals of an egg incubator if an entity is removed from its contents.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnIncubatorContentsRemoved(Entity<EggIncubatorComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        UpdateIncubatorVisuals(entity);
    }

    /// <summary>
    ///     Handles hatching our egg once the time has passed and creating the new mob.
    /// </summary>
    /// <param name="entity">The incubator hatching our mob</param>
    private void FinishIncubation(Entity<IncubationComponent> entity)
    {
        var newMob = SpawnNewMob(entity, entity.Comp.IncubatedResult);

        if (TryComp<InteractionPopupComponent>(newMob, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates((EntityUid)newMob));

        PredictedDel(entity.Owner);
    }

    /// <summary>
    ///     Handles spawning a new mob for us
    /// </summary>
    /// <param name="entity">Entity calling the creation</param>
    /// <param name="toSpawn">What entity will be spawned</param>
    /// <returns>The ID of our new mob! Or nothing if for some reason it didn't spawn.</returns>
    private EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);
        var newMob = PredictedSpawnAtPosition(toSpawn, xform.Coordinates);

        return newMob;
    }

    /// <summary>
    ///     Update the "last updated" time of all eggs inside this incubator.
    /// </summary>
    /// <remarks>
    ///     We do this when the incubator is switched on/off, otherwise eggs will
    ///     gain a lot of incubation time at once if the incubator is turned off then on.
    /// </remarks>
    /// <param name="entity">The egg incubator.</param>
    private void UpdateIncubatorContents(Entity<EggIncubatorComponent> entity)
    {
        if (!_container.TryGetContainer(entity.Owner, entity.Comp.ContainerId, out var container))
            return;

        foreach (var egg in container.ContainedEntities)
        {
            if (TryComp<IncubationComponent>(egg, out var incubation))
                incubation.LastUpdateTime = _time.CurTime;
        }
    }

    /// <summary>
    ///     Updates the visual status of an egg incubator based on its powered status.
    /// </summary>
    /// <param name="entity">The egg incubator to update.</param>
    /// <param name="powered">Optional, a provided "powered" status of the incubator.</param>
    private void UpdateIncubatorVisuals(Entity<EggIncubatorComponent> entity)
    {
        var canIncubate = CanIncubate(entity.AsNullable());
        var hasContents = IncubatorHasContents(entity.AsNullable());

        var status = canIncubate && hasContents
            ? IncubatorStatus.Active
            : IncubatorStatus.Inactive;

        // Don't update the visuals if it's identical, anyway.
        if (_appearance.TryGetData(entity.Owner, IncubatorVisualizerLayers.Status, out var currentStatus)
            && status.Equals(currentStatus))
            return;

        _appearance.SetData(entity.Owner, IncubatorVisualizerLayers.Status, status);
    }

    /// <summary>
    ///     Gets whether or not an egg incubator is currently capable of incubating.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    /// <returns>Whether or not the incubator is currently capable of incubating.</returns>
    private bool CanIncubate(Entity<EggIncubatorComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        return _powerSystem.IsPowered(entity.Owner);
    }

    /// <summary>
    ///     Gets whether or not an egg incubator is currently holding anything.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    /// <returns>Whether or not an egg incubator is currently holding anything.</returns>
    public bool IncubatorHasContents(Entity<EggIncubatorComponent?> entity)
    {
        // Not an incubator.
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        // Lacks an incubation container.
        if (!_container.TryGetContainer(entity.Owner, entity.Comp.ContainerId, out var container))
            return false;

        // For shits and giggles, I'm gonna say "yes" if the incubation container contains anything -
        // even if it's not an egg. Because how would it know, anyway?
        return container.Count > 0;
    }

    /// <summary>
    ///     Gets whether or not an incubating entity is actively being incubated.
    /// </summary>
    /// <param name="entity">The incubating entity.</param>
    /// <returns>Whether or not the incubating entity is being incubated.</returns>
    public bool IsBeingIncubated(Entity<IncubationComponent> entity)
    {
        var xform = Transform(entity.Owner);

        // Is this entity inside an active incubator?
        if (_container.TryGetOuterContainer(entity.Owner, xform, out var container)
            && CanIncubate(container.Owner))
            return true;

        return false;
    }
}
