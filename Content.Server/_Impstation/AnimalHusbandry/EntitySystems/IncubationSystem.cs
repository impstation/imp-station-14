using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.StationEvents;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.AnimalHusbandry.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;
/// <summary>
/// Handles the system for incubating eggs or whatever else you want incubatable.
/// </summary>
public sealed class IncubationSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    //[Dependency] private readonly SharedIncubationSystem _incubationSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, InteractUsingEvent>(OnAfterInteract);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Just run through and check on all the incubators
        var query = EntityQueryEnumerator<IncubatorComponent>();
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

    private void OnAfterInteract(Entity<IncubatorComponent> entity, ref InteractUsingEvent args)
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
    public void FinishIncubation(Entity<IncubatorComponent> entity)
    {
        // This should never be false unless an admin decides it should be
        if (!_entManager.TryGetComponent<ApcPowerReceiverComponent>(entity, out var powerComp))
            return;

        // Making sure we have a container. Should also never be false without admin shenanigans.
        if (TryComp<ItemSlotsComponent>(entity, out var container))
            return;

        // If the incubator is not powered
        if (!powerComp.Powered)
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

    public EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);

        var newMob = Spawn(toSpawn, xform.Coordinates);

        return newMob;
    }

    public void CheckPowerchanged(Entity<IncubatorComponent> entity)
    {
        if (!_entManager.TryGetComponent<ApcPowerReceiverComponent>(entity, out var powerComp))
            return;

        if(powerComp.Powered && entity.Comp.Status == IncubatorStatus.Inactive)
        {
            _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Active);
            entity.Comp.Status = IncubatorStatus.Active;
        }
        else if (!powerComp.Powered && entity.Comp.Status == IncubatorStatus.Active)
        {
            _appearance.SetData(entity, IncubatorVisualizerLayers.Status, IncubatorStatus.Inactive);
            entity.Comp.Status = IncubatorStatus.Inactive;
        }
    }
}
