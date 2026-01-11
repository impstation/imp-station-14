using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Impstation.Nutrition.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Nutrition.EntitySystems;
public sealed class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    Dictionary<EntityUid, ImpReproductiveComponent> _mobsWaiting = new Dictionary<EntityUid, ImpReproductiveComponent>();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // This is so the HTN system isn't in this weird state of trying to give birth as much as it can
        // it allows to easily keep track of all the pregnant mobs at a given time and
        // could potentially be used for the genetic trait system animals may have (similar to plants)
        // Whether it stays or not depends on if it's better here or in an HTN. I would like it to be in an
        // HTN, but also, HTN.
        foreach (var reproComp in _mobsWaiting)
        {
            if(reproComp.Value == null || reproComp.Key == EntityUid.Invalid)
            {
                _mobsWaiting.Remove(reproComp.Key);
                continue;
            }

            if (_time.CurTime > reproComp.Value.EndPregnancy)
            {
                reproComp.Value.Pregnant = false;
                Birth((reproComp.Key, reproComp.Value));
                _mobsWaiting.Remove(reproComp.Key);
                _adminLog.Add(LogType.Action, $"A mob has given birth!");
            }
        }
    }

    /// <summary>
    /// Our function for handling the breeding action once all checks are finished and the
    /// animal has approached its partner.
    /// </summary>
    /// <param name="approacher"></param>
    /// <param name="approached"></param>
    /// <returns></returns>
    public bool TryBreedWithTarget(EntityUid approacher, EntityUid approached)
    {
        // Realistically this should never return false but it's just here for the moment
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(approacher, out var component))
            return false;

        // Same with this one but i'm paranoid
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(approached, out var partnerComp))
            return false;

        // Just being 100% sure we aren't trying to self breed.
        if (approacher == approached)
            return false;

        // one last check in case someone beat us to the cow or bred us on the way there
        if (!CanYouBreed((approacher, component)) || !CanYouBreed((approached, partnerComp)))
            return false;

        // Ready up for birth
        partnerComp.Pregnant = true;
        partnerComp.EndPregnancy = _time.CurTime + partnerComp.PregnancyLength;

        // Add them to our list of pregnant NPCs to be tracked
        _mobsWaiting.Add(approached, partnerComp);

        // This is temporary
        _popup.PopupEntity("BREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBRED", approached);

        // GET HUNGRY GET THIRSTY
        _hunger.ModifyHunger(approacher, -component.HungerPerBirth);
        _hunger.ModifyHunger(approached, -partnerComp.HungerPerBirth);

        _thirst.ModifyThirst(approached, -component.HungerPerBirth);
        _thirst.ModifyThirst(approached, -partnerComp.HungerPerBirth);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(approacher)} (carrier) and {ToPrettyString(approached)} (partner) successfully bred.");
        return true;
    }

    /// <summary>
    /// Checking if a given entity meets the criteria for breeding
    /// This isn't set in stone to remain and it largely in this class for the moment so multiple scripts
    /// Can use it, whether it remains is yet to be discovered. Depends on if it needs to be.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool CanYouBreed(Entity<ImpReproductiveComponent> entity)
    {
        if (entity.Comp.Pregnant)
            return false;

        if (_entManager.TryGetComponent<HungerComponent>(entity, out var hunger) && hunger.CurrentThreshold < HungerThreshold.Okay)
            return false;

        if (_entManager.TryGetComponent<ThirstComponent>(entity, out var thirst) && thirst.CurrentThirstThreshold < ThirstThreshold.Okay)
            return false;

        return true;
    }

    /// <summary>
    /// Same as CanYouBreed except this one takes into account the animals search times
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool CanIBreed(Entity<ImpReproductiveComponent> entity)
    {
        if (entity.Comp.NextSearch > _time.CurTime)
            return false;

        if (!CanYouBreed(entity))
            return false;

        return true;
    }

    /// <summary>
    /// Handles the actual birthing of the new NPC
    /// </summary>
    /// <param name="entity"></param>
    public void Birth(Entity<ImpReproductiveComponent> entity)
    {
        if (TryComp<InteractionPopupComponent>(entity, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, entity);

        _popup.PopupEntity("BIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTH", entity);
    }
}
