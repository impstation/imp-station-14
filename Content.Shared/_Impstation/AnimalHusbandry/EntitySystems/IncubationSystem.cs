using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
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
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    bool _justSwapped = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggIncubatorComponent, ComponentStartup>(OnEggIncubatorStartup);
        SubscribeLocalEvent<EggIncubatorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<EggIncubatorComponent, InteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<EggIncubatorComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
    }

    /// <summary>
    /// Goes through and checks on each active incubator to see if it's time to hatch
    /// </summary>
    /// <param name="frameTime">Time between frames</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Just run through and check on all the incubators
        var query = EntityQueryEnumerator<EggIncubatorComponent>();
        while (query.MoveNext(out var uid, out var incuComp))
        {
            // Making sure we're incubating something
            if (!IsCurrentlyIncubating((uid, incuComp)))
                continue;

            if (incuComp.Status != IncubatorStatus.Active)
                incuComp.FinishIncubation = incuComp.FinishIncubation.Add(_time.FrameTime);

            // Hatch
            if (incuComp.FinishIncubation <= _time.CurTime)
                FinishIncubation((uid, incuComp));
        }
    }

    /// <summary>
    ///     Update the visuals of an egg incubator on startup, if it is incubating something.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnEggIncubatorStartup(Entity<EggIncubatorComponent> entity, ref ComponentStartup args)
    {
        if (IsCurrentlyIncubating(entity))
            UpdatePowerVisuals(entity);
    }

    /// <summary>
    ///     Update the visuals of an egg incubator if its power status changes during incubation.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    private void OnPowerChanged(Entity<EggIncubatorComponent> entity, ref PowerChangedEvent args)
    {
        if (IsCurrentlyIncubating(entity))
            UpdatePowerVisuals(entity, args.Powered);
    }

    /// <summary>
    /// Called whenever someone tries to place something into the Incubator
    /// If it's valid, we'll store it and begin the hatch timer
    /// </summary>
    /// <param name="entity">The Incubator being touched</param>
    /// <param name="args">Our arguments for the event</param>
    private void OnAfterInteract(Entity<EggIncubatorComponent> entity, ref InteractUsingEvent args)
    {
        // If it can't be incubated, cancel
        if (!_entManager.TryGetComponent<IncubationComponent>(args.Used, out var incuComp))
            return;

        if (entity.Comp.CurrentlyIncubated != null)
            _justSwapped = true;

        SetIncubatorStatus(entity, IncubatorStatus.Active);
        entity.Comp.CurrentlyIncubated = (args.Used, incuComp);
        entity.Comp.FinishIncubation = incuComp.IncubationTime.Add(_time.CurTime);
    }

    /// <summary>
    /// Handles updating our incubator whenever the egg is removed
    /// This wipes the reference to the egg so the incubator isn't still trying to incubate even after it's gone
    /// </summary>
    /// <param name="entity">The incubator we're removing from</param>
    /// <param name="args">Our arguments for the event</param>
    private void OnEntityRemoved(Entity<EggIncubatorComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // We weren't even incubating so just ignore whatever is going on
        if (entity.Comp.CurrentlyIncubated == null)
            return;

        // Prevents us totally wiping the incubator if you swapped a new egg in
        if (_justSwapped)
        {
            _justSwapped = false;
            return;
        }

        SetIncubatorStatus(entity, IncubatorStatus.Active);
        _containerSystem.RemoveEntity(entity, entity.Comp.CurrentlyIncubated.Value.Owner);
        entity.Comp.CurrentlyIncubated = null;
        entity.Comp.FinishIncubation = TimeSpan.Zero;
    }

    /// <summary>
    /// Handles hatching our egg once the time has passed and creating the new mob.
    /// </summary>
    /// <param name="entity">The incubator hatching our mob</param>
    private void FinishIncubation(Entity<EggIncubatorComponent> entity)
    {
        // Making sure we have a container. Should also never be false without admin shenanigans.
        if (!TryComp<ItemSlotsComponent>(entity, out var container))
            return;

        var incubated = entity.Comp.CurrentlyIncubated;
        if (incubated == null)
            return;

        var newMob = SpawnNewMob(entity, incubated.Value.Comp.IncubatedResult);
        SetIncubatorStatus(entity, IncubatorStatus.Active);

        if (TryComp<InteractionPopupComponent>(newMob, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates((EntityUid)newMob));

        _containerSystem.RemoveEntity(entity, incubated.Value.Owner);
        _entManager.PredictedDeleteEntity(incubated.Value.Owner);
    }

    /// <summary>
    /// Handles spawning a new mob for us
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
    ///     Updates the visual status of an egg incubator based on its powered status.
    /// </summary>
    /// <param name="entity">The egg incubator to update.</param>
    /// <param name="powered">Optional, a provided "powered" status of the incubator.</param>
    private void UpdatePowerVisuals(Entity<EggIncubatorComponent> entity, bool? powered = null)
    {
        powered ??= _powerSystem.IsPowered(entity.Owner);
        var status = powered.Value
            ? IncubatorStatus.Active
            : IncubatorStatus.Inactive;

        if (status != entity.Comp.Status)
            SetIncubatorStatus(entity, status);
    }

    /// <summary>
    ///     Set the status of an egg incubator, updating its visuals.
    /// </summary>
    /// <param name="entity">The egg incubator.</param>
    /// <param name="status">The status of the incubator.</param>
    private void SetIncubatorStatus(Entity<EggIncubatorComponent> entity, IncubatorStatus status)
    {
        entity.Comp.Status = status;
        _appearance.SetData(entity.Owner, IncubatorVisualizerLayers.Status, status);
    }

    /// <summary>
    ///     Gets whether or not this incubator is currently incubating an egg.
    /// </summary>
    /// <param name="entity">The egg incubator entity.</param>
    /// <returns>Whether or not this incubator is currently incubating an egg</returns>
    private bool IsCurrentlyIncubating(Entity<EggIncubatorComponent> entity)
    {
        return entity.Comp.CurrentlyIncubated != null
            && !TerminatingOrDeleted(entity.Comp.CurrentlyIncubated);
    }
}
