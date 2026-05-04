using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
/// Handles the system for incubating eggs or whatever else you want incubatable.
/// </summary>
public sealed class IncubationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggIncubatorComponent, ComponentStartup>(OnEggIncubatorStartup);
        SubscribeLocalEvent<EggIncubatorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<EggIncubatorComponent, EntInsertedIntoContainerMessage>(OnIncubatorContentsInserted);
        SubscribeLocalEvent<EggIncubatorComponent, EntRemovedFromContainerMessage>(OnIncubatorContentsRemoved);
        SubscribeLocalEvent<IncubatableComponent, IsGestatingEvent>(IsIncubatableGestating);
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
    ///     Prevents an incubatable entity from gestating if it is not being incubated.
    /// </summary>
    /// <param name="entity">The incubatable entity.</param>
    private void IsIncubatableGestating(Entity<IncubatableComponent> entity, ref IsGestatingEvent args)
    {
        if (!IsBeingIncubated(entity))
            args.Cancelled = true;
    }

    /// <summary>
    ///     Updates the visual status of an egg incubator based on its powered status.
    /// </summary>
    /// <param name="entity">The egg incubator to update.</param>
    private void UpdateIncubatorVisuals(Entity<EggIncubatorComponent> entity)
    {
        var canIncubate = IncubatorCanIncubate(entity.AsNullable());
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
    public bool IncubatorCanIncubate(Entity<EggIncubatorComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        return _powerSystem.IsPowered(entity.Owner);
    }

    /// <summary>
    ///     Gets whether or not an egg incubator is currently holding anything.
    /// </summary>
    /// <remarks>
    ///     The contents may not necessarily be incubatable entities. It could hold a bunch of non-egg entities.
    /// </remarks>
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
    ///     Gets whether or not an entity is actively being incubated.
    /// </summary>
    /// <param name="uid">An entity to check for incubation.</param>
    /// <returns>Whether or not the incubating entity is being incubated.</returns>
    public bool IsBeingIncubated(EntityUid uid)
    {
        var xform = Transform(uid);

        // Is this entity inside an active incubator?
        if (_container.TryGetOuterContainer(uid, xform, out var container)
            && IncubatorCanIncubate(container.Owner))
            return true;

        return false;
    }
}
