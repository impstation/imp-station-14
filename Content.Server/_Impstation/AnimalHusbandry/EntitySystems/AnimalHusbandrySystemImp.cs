using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Cloning;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.EntityTable.Conditions;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Damage.Components;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;
/// <summary>
/// System that handles mob breeding, birthing and growing
/// This system works alongside HTN
/// </summary>
public sealed class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    Dictionary<EntityUid, ImpReproductiveComponent> _mobsWaiting = new Dictionary<EntityUid, ImpReproductiveComponent>();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Runs through the list of pregnant mobs to see who is ready to give birth
        // or whose birth should be cancelled
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
            // Same for if the animal becomes player controlled
            if (state.CurrentState != Shared.Mobs.MobState.Alive ||
                _mind.TryGetMind(reproComp.Key, out var mind, out var mindComp))
            {
                _mobsWaiting.Remove(reproComp.Key);
                reproComp.Value.Pregnant = false;
            }

            reproComp.Value.EndPregnancy -= frameTime;

            // Is it time to give birth?
            if (reproComp.Value.EndPregnancy <= 0)
            {
                reproComp.Value.Pregnant = false;
                Birth((reproComp.Key, reproComp.Value));
                _mobsWaiting.Remove(reproComp.Key);
                _adminLog.Add(LogType.Action, $"A mob has given birth!");
            }
        }

        // Grabs every single entity with this component and runs through them all
        // to see who should grow up.
        // Realistically there should never be THAT many infants, so this approach should be
        // fine for performance.
        var query = EntityQueryEnumerator<ImpInfantComponent>();
        while (query.MoveNext(out var uid, out var infantComp))
        {
            if (_entManager.Deleted(uid)
                || !_entManager.TryGetComponent<MobStateComponent>(uid, out var state))
                continue;

            infantComp.GrowthTimeRemaining -= frameTime;

            if (infantComp.GrowthTimeRemaining <= 0)
                AdvanceStage((uid, infantComp));
        }
    }

    #region BIRTHING

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

        // The one giving birth needs to have options
        if (partnerComp.PossibleInfants == null)
            return false;

        // one last check in case someone beat us to the cow or bred us on the way there
        // It's dumb but unless I make the system assign pairings this is the best i can think of for the moment
        if (!CanYouBreed((approacher, component)) || !CanYouBreed((approached, partnerComp)))
            return false;

        // Ready up for birth
        partnerComp.Pregnant = true;
        partnerComp.EndPregnancy = partnerComp.PregnancyLength;

        // Add them to our list of pregnant NPCs to be tracked
        _mobsWaiting.Add(approached, partnerComp);

        // Picks which offspring to give birth to based on the mob we bred with
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { ValidPartnerCondition.PartnerContextKey, component.MobType },
        });
        partnerComp.MobToBirth = _entTable.GetSpawns(partnerComp.PossibleInfants, ctx: ctx).ElementAt(0);

        if (TryComp<InteractionPopupComponent>(approached, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates(approached));

        partnerComp.PreviousPartner = approacher;

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

        // Player inhabited mobs cannot breed
        if (_mind.TryGetMind(entity, out var mind, out var mindComp))
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
    /// Handles the actual birthing of the new NPC and sets how long until they grow up
    /// </summary>
    /// <param name="entity"></param>
    private void Birth(Entity<ImpReproductiveComponent> entity)
    {
        if (TryComp<InteractionPopupComponent>(entity, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, entity);

        var offspring = SpawnNewMob(entity, entity.Comp.MobToBirth);

        if (offspring == null)
            return;

        if (_entManager.TryGetComponent<ImpInfantComponent>(offspring, out var infantComp))
        {
            infantComp.GrowthTimeRemaining = infantComp.GrowthTime;
            infantComp.Parent = entity;
        }
    }

    #endregion

    #region INFANTS

    /// <summary>
    /// Handles growing an infant by deleting the current mob and making a new one
    /// This also transfers the previous components to the new mob
    /// </summary>
    /// <param name="_infant"></param>
    /// <returns></returns>
    private bool AdvanceStage(Entity<ImpInfantComponent> infant)
    {
        bool isAdult = false;

        var newStage = SpawnNewMob(infant, infant.Comp.NextStage);

        if (newStage == null)
            return false;

        if (!_prototype.Resolve(infant.Comp.OffspringSettings, out var settings))
            return false;

        // If someone is in this thing, move them over as well
        if (_mind.TryGetMind(infant, out var mind, out var mindComp))
            _mind.TransferTo(mind, newStage);

        isAdult = !_entManager.TryGetComponent<InfantComponent>(newStage, out var comp);

        // Make sure the relevant Data like damage carries over
        _cloning.CloneComponents(infant, (EntityUid)newStage, settings);

        // If there is a ghost role attached to this mob, keep it
        if (_entManager.TryGetComponent<GhostRoleComponent>(infant, out var ghostComp))
        {
            AddComp<GhostRoleComponent>((EntityUid)newStage);
            CopyComp(infant, (EntityUid)newStage, ghostComp);
        }

        QueueDel(infant);

        // So they don't immediately try to breed the second they grow up
        if(isAdult && _entManager.TryGetComponent<ImpReproductiveComponent>(newStage, out var reproComp))
            reproComp.NextSearch = _time.CurTime + _random.Next(reproComp.MinSearchAttemptInterval, reproComp.MaxSearchAttemptInterval);

        return isAdult;
    }

    #endregion

    #region UNIVERSAL

    public EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);

        var newMob = Spawn(toSpawn, xform.Coordinates);

        return newMob;
    }

    #endregion
}
