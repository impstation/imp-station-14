using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Power.EntitySystems;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggIncubatorComponent, InteractUsingEvent>(OnAfterInteract);
    }

    /// <summary>
    /// Goes through and checks on each active incubator to see if it's time to hatch
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Just run through and check on all the incubators
        var query = EntityQueryEnumerator<EggIncubatorComponent>();
        while(query.MoveNext(out var uid, out var incuComp))
        {
            // Making sure we're incubating something
            if (incuComp.CurrentlyIncubated == null || _entManager.Deleted(incuComp.CurrentlyIncubated.Owner))
                continue;

            CheckPowerchanged((uid, incuComp));

            // Wait
            if (incuComp.FinishIncubation > _time.CurTime)
                continue;

            FinishIncubation((uid, incuComp));
        }
    }

    /// <summary>
    /// Called whenever someone tries to place something into the Incubator
    /// If it's valid, we'll store it and begin the hatch timer
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnAfterInteract(Entity<EggIncubatorComponent> entity, ref InteractUsingEvent args)
    {
        // If it can't be incubated, cancel
        if (!_entManager.TryGetComponent<IncubationComponent>(args.Used, out var incuComp))
            return;

        _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Active);
        entity.Comp.Status = IncubatorStatus.Active;
        entity.Comp.CurrentlyIncubated = incuComp;
        entity.Comp.FinishIncubation = _time.CurTime + incuComp.IncubationTime;
    }

    /// <summary>
    /// Handles hatching our egg once the time has passed and creating the new mob.
    /// </summary>
    /// <param name="entity"></param>
    private void FinishIncubation(Entity<EggIncubatorComponent> entity)
    {
        // Making sure we have a container. Should also never be false without admin shenanigans.
        if (!TryComp<ItemSlotsComponent>(entity, out var container))
            return;

        // If the incubator is not powered
        if (entity.Comp.Status == IncubatorStatus.Inactive)
        {
            entity.Comp.FinishIncubation = _time.CurTime + (entity.Comp.CurrentlyIncubated.IncubationTime / 2);
            return;
        }

        var newMob = SpawnNewMob(entity, entity.Comp.CurrentlyIncubated.IncubatedResult);
        var incubated = entity.Comp.CurrentlyIncubated.Owner;

        _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Inactive);
        entity.Comp.Status = IncubatorStatus.Inactive;

        if (TryComp<InteractionPopupComponent>(newMob, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates((EntityUid)newMob));


        _entManager.QueueDeleteEntity(incubated);
    }

    private EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);

        var newMob = Spawn(toSpawn, xform.Coordinates);

        return newMob;
    }

    /// <summary>
    /// Check if we're still powered and based on our activity, update the Incubators Status and visuals as needed
    /// </summary>
    /// <param name="entity"></param>
    private void CheckPowerchanged(Entity<EggIncubatorComponent> entity)
    {
        var powered = _powerSystem.IsPowered(entity.Owner);

        if(powered && entity.Comp.Status == IncubatorStatus.Inactive)
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
