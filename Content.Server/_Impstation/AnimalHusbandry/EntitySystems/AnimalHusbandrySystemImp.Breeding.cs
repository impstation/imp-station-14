using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;

using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.EntityTable.Conditions;
using System.Linq;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    /// <summary>
    /// Our function for handling the breeding action once all checks are finished and the
    /// animal has approached its partner.
    /// </summary>
    /// <param name="approacher">The mob seeking to impregnate</param>
    /// <param name="approached">The mob that will be impregnated</param>
    /// <returns>True if the mob has successfully bred with the target</returns>
    public bool TryBreedWithTarget(Entity<ImpReproductiveComponent?> approacher, Entity<ImpReproductiveComponent?> approached)
    {
        var approacherProto = MetaData(approacher.Owner).EntityPrototype;

        if (approacher == approached // Do not self-breed
            || !Resolve(approacher.Owner, ref approacher.Comp) // Ensure approacher can reproduce
            || !Resolve(approached.Owner, ref approached.Comp) // Ensure partner can reproduce
            || approacherProto == null) // Ensure approacher has a prototype
            return false;

        var component = approacher.Comp;
        var partnerComp = approached.Comp;

        // one last check in case someone beat us to the cow or bred us on the way there
        // It's dumb but unless I make the system assign pairings this is the best i can think of for the moment
        if (!CanYouBreed((approacher, component)) || !CanYouBreed((approached, partnerComp)))
            return false;

        if (!_prototype.TryIndex(partnerComp.BreedSettings, out var partnerSettings))
            return false;

        // Ready up for birth
        partnerComp.Pregnant = true;
        partnerComp.EndPregnancy = _time.CurTime.Add(partnerComp.PregnancyLength);

        // Add them to our list of pregnant NPCs to be tracked
        _mobsWaiting.Add(approached, partnerComp);

        // Picks which offspring to give birth to based on the mob we bred with
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { ValidPartnerCondition.PartnerContextKey, approacherProto.ID },
        });
        partnerComp.MobToBirth = _entTable.GetSpawns(partnerSettings.PossibleInfants, ctx: ctx).ElementAt(0);

        if (TryComp<InteractionPopupComponent>(approached, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates(approached));

        partnerComp.PreviousPartner = approacher;

        // GET HUNGRY GET THIRSTY
        _hunger.ModifyHunger(approacher, -component.HungerPerBirth);
        _hunger.ModifyHunger(approached, -partnerComp.HungerPerBirth);

        _thirst.ModifyThirst(approacher, -component.HungerPerBirth);
        _thirst.ModifyThirst(approached, -partnerComp.HungerPerBirth);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(approacher)} (carrier) and {ToPrettyString(approached)} (partner) successfully bred.");
        return true;
    }

    /// <summary>
    /// Checking if a given entity meets the criteria for breeding
    /// This isn't set in stone to remain and it largely in this class for the moment so multiple scripts
    /// Can use it, whether it remains is yet to be discovered. Depends on if it needs to be.
    /// </summary>
    /// <param name="entity">The fella we're checking out. Or ourself</param>
    /// <returns>If the mob is eligible to breed</returns>
    public bool CanYouBreed(Entity<ImpReproductiveComponent> entity)
    {
        if (entity.Comp.Pregnant)
            return false;

        // Making sure we're not trying to breed with an infant
        if (_entManager.TryGetComponent<ImpInfantComponent>(entity, out var infant))
            return false;

        // Player inhabited mobs cannot breed
        if (_mind.TryGetMind(entity, out var mind, out var mindComp))
            return false;

        if (_entManager.TryGetComponent<HungerComponent>(entity, out var hunger) && hunger.CurrentThreshold < HungerThreshold.Okay)
            return false;

        if (_entManager.TryGetComponent<ThirstComponent>(entity, out var thirst) && thirst.CurrentThirstThreshold < ThirstThreshold.Okay)
            return false;

        // A mob needs to be Alive. Not dead or critical
        if (_entManager.TryGetComponent<MobStateComponent>(entity, out var state) && state.CurrentState != Shared.Mobs.MobState.Alive)
            return false;

        // A mob can't be too damaged
        if (_entManager.TryGetComponent<DamageableComponent>(entity, out var damage) && damage.TotalDamage >= entity.Comp.MaxBreedDamage)
            return false;
        return true;
    }

    /// <summary>
    /// Same as CanYouBreed except this one takes into account the animals search times
    /// </summary>
    /// <param name="entity">The mob checking if it's eligible to breed</param>
    /// <returns>If the mob is eligible for breeding</returns>
    public bool CanIBreed(Entity<ImpReproductiveComponent> entity)
    {
        if (entity.Comp.NextSearch > _time.CurTime)
            return false;

        if (!CanYouBreed(entity))
            return false;

        return true;
    }

    /// <summary>
    /// Handles the actual birthing of the new NPC and sets how long until they grow up
    /// </summary>
    /// <param name="entity">The mob giving birth</param>
    private void Birth(Entity<ImpReproductiveComponent> entity)
    {
        if (TryComp<InteractionPopupComponent>(entity, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, entity);

        var offspring = SpawnNewMob(entity, entity.Comp.MobToBirth);

        if (offspring == null)
            return;

        if (_entManager.TryGetComponent<ImpInfantComponent>(offspring, out var infantComp))
        {
            infantComp.GrowthTimeRemaining = _time.CurTime.Add(infantComp.GrowthTime);
            infantComp.Parent = entity;
        }
    }
}
