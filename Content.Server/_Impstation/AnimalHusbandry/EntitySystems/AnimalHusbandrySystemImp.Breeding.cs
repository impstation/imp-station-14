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

        var partnerComp = approached.Comp;

        // one last check in case someone beat us to the cow or bred us on the way there
        // It's dumb but unless I make the system assign pairings this is the best i can think of for the moment
        if (!CanYouBreed(approacher) || !CanYouBreed(approached))
            return false;

        if (!_prototype.TryIndex(partnerComp.BreedSettings, out var partnerSettings))
            return false;

        // Picks which offspring to give birth to based on the mob we bred with
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { ValidPartnerCondition.PartnerContextKey, approacherProto.ID },
        });

        // Add gestation to the approached mob
        var gestating = EnsureComp<GestatingComponent>(approached.Owner);
        gestating.GestationTime = partnerComp.PregnancyLength;
        gestating.EntityToSpawn = _entTable.GetSpawns(partnerSettings.PossibleInfants, ctx: ctx).First();
        partnerComp.PreviousPartner = approacher;

        if (TryComp<InteractionPopupComponent>(approached, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates(approached));

        // GET HUNGRY GET THIRSTY
        _hunger.ModifyHunger(approacher, -approacher.Comp.HungerPerBirth);
        _hunger.ModifyHunger(approached, -partnerComp.HungerPerBirth);

        _thirst.ModifyThirst(approacher, -approacher.Comp.HungerPerBirth);
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
    public bool CanYouBreed(Entity<ImpReproductiveComponent?> entity)
    {
        // Not a reproductive entity.
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        // Already gestating.
        if (HasComp<GestatingComponent>(entity.Owner))
            return false;

        // Incapable of gestation.
        if (!IsUnableToGestate(entity.Owner))
            return false;

        // Too hungry.
        if (TryComp<HungerComponent>(entity, out var hunger)
            && hunger.CurrentThreshold < entity.Comp.MinimumHungerThreshold)
            return false;

        // Too thirsty.
        if (TryComp<ThirstComponent>(entity, out var thirst)
            && thirst.CurrentThirstThreshold < entity.Comp.MinimumThirstThreshold)
            return false;

        // Too injured.
        if (TryComp<DamageableComponent>(entity, out var damage)
            && damage.TotalDamage >= entity.Comp.MaxBreedDamage)
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

        if (!CanYouBreed(entity.AsNullable()))
            return false;

        return true;
    }
}
