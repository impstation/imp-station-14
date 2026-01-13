using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Impstation.Nutrition.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
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

    int _framesUntilNextBithCheck = 0;
    int _birthCheckFrameDelay = 6;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // This approach is one i'm still debating because there's pros and cons
        // Pros are this effectively means rather than each mob checking if it is ready to give birth
        // one single system KNOWS what mobs need to ask that
        // Cons are seperation from the HTN (Part of this project was to make sure the HTN is actually USED LIKE IT'S MEANT TO BE)

        // The delay largely exists for performance reasons since realistically we do not need frame accurate
        // mob births. They can afford to be a little late.
        if (_framesUntilNextBithCheck <= 0)
        {
            _framesUntilNextBithCheck = _birthCheckFrameDelay;
            foreach (var reproComp in _mobsWaiting)
            {
                // In case the mob finds itself deleted or destroyed
                if (_entManager.Deleted(reproComp.Key)
                    || !_entManager.TryGetComponent<MobStateComponent>(reproComp.Key, out var state))
                {
                    _mobsWaiting.Remove(reproComp.Key);
                    continue;
                }

                // If they die or become critical, the child is gone
                if (state.CurrentState != Shared.Mobs.MobState.Alive)
                {
                    _mobsWaiting.Remove(reproComp.Key);
                    reproComp.Value.Pregnant = false;
                }

                // Is it time to give birth?
                if (_time.CurTime > reproComp.Value.EndPregnancy)
                {
                    reproComp.Value.Pregnant = false;
                    Birth((reproComp.Key, reproComp.Value));
                    _mobsWaiting.Remove(reproComp.Key);
                    _adminLog.Add(LogType.Action, $"A mob has given birth!");
                }
            }
        }
        else
        {
            _framesUntilNextBithCheck--;
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
        // It's dumb but unless I make the system assign pairings this is the best i can think of for the moment
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

        /*
        if (_entManager.TryGetComponent<HungerComponent>(entity, out var hunger) && hunger.CurrentThreshold < HungerThreshold.Okay)
            return false;

        if (_entManager.TryGetComponent<ThirstComponent>(entity, out var thirst) && thirst.CurrentThirstThreshold < ThirstThreshold.Okay)
            return false;

        if (_entManager.TryGetComponent<MobStateComponent>(entity, out var state) && state.CurrentState != Shared.Mobs.MobState.Alive)
            return false;

        if (_entManager.TryGetComponent<DamageableComponent>(entity, out var damage) && damage.TotalDamage >= entity.Comp.MaxBreedDamage)
            return false;
        */
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

        // This is also temporary
        _popup.PopupEntity("BIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTH", entity);
    }
}
