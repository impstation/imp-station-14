using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
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
            if (incuComp.CurrentlyIncubated == null || _entManager.Deleted(incuComp.CurrentlyIncubated.Owner))
                continue;

            CheckPowerchanged((uid, incuComp));

            if (incuComp.Status != IncubatorStatus.Active)
                incuComp.FinishIncubation = incuComp.FinishIncubation.Add(_time.FrameTime);

            // Hatch
            if (incuComp.FinishIncubation <= _time.CurTime)
                FinishIncubation((uid, incuComp));
        }
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

        _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Active);
        entity.Comp.Status = IncubatorStatus.Active;
        entity.Comp.CurrentlyIncubated = incuComp;
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
        if(_justSwapped)
        {
            _justSwapped = false;
            return;
        }

        _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Inactive);
        entity.Comp.Status = IncubatorStatus.Inactive;
        _containerSystem.RemoveEntity(entity, entity.Comp.CurrentlyIncubated.Owner);
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

        if (entity.Comp.CurrentlyIncubated == null)
            return;

        var newMob = SpawnNewMob(entity, entity.Comp.CurrentlyIncubated.IncubatedResult);
        var incubated = entity.Comp.CurrentlyIncubated.Owner;

        _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Inactive);
        entity.Comp.Status = IncubatorStatus.Inactive;

        if (TryComp<InteractionPopupComponent>(newMob, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates((EntityUid)newMob));

        _containerSystem.RemoveEntity(entity, incubated);
        _entManager.PredictedDeleteEntity(incubated);
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
    /// Check if we're still powered and based on our activity, update the Incubators Status and visuals as needed
    /// </summary>
    /// <param name="entity">The incubator we're checking in on</param>
    private void CheckPowerchanged(Entity<EggIncubatorComponent> entity)
    {
        var powered = _powerSystem.IsPowered(entity.Owner);

        if (powered && entity.Comp.Status == IncubatorStatus.Inactive)
        {
            _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Active);
            entity.Comp.Status = IncubatorStatus.Active;
        }
        else if (!powered && entity.Comp.Status == IncubatorStatus.Active)
        {
            _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Inactive);
            entity.Comp.Status = IncubatorStatus.Inactive;
        }
    }
}
