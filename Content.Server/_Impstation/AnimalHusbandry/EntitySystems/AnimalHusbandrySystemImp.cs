using Content.Server.Administration.Logs;
using Content.Shared.EntityTable;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Nutrition.Components;
using System.Linq;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.EntityTable.Conditions;
using Content.Shared._Impstation.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
///     This system handles animal breading, used in conjunction with HTN.
/// </summary>
public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;
    [Dependency] private readonly AnimalGestationSystem _gestation = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public int MaxBreedableAnimalsCount = 5;
    public float BreedableLimitRange = 10.0f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config,
            ImpCCVars.MaxBreedableAnimalsCount,
            value =>
            {
                if (value < 0)
                {
                    MaxBreedableAnimalsCount = -1;
                    return;
                }

                MaxBreedableAnimalsCount = value;
            },
            invokeImmediately: true);

        Subs.CVar(_config,
            ImpCCVars.BreedableLimitRange,
            value =>
            {
                if (value < 0.0f)
                {
                    BreedableLimitRange = -1.0f;
                    return;
                }

                BreedableLimitRange = value;
            },
            invokeImmediately: true);
    }

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
            || !Resolve(approacher.Owner, ref approacher.Comp, logMissing: false) // Ensure approacher can reproduce
            || !Resolve(approached.Owner, ref approached.Comp, logMissing: false) // Ensure partner can reproduce
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

        var spawns = _entTable.GetSpawns(partnerSettings.PossibleInfants, ctx: ctx);
        if (!spawns.Any()) // No valid offspring
            return false;

        // Add gestation to the approached mob
        var gestating = EnsureComp<GestatingComponent>(approached.Owner);
        gestating.GestationTime = partnerComp.PregnancyLength;
        gestating.EntityToSpawn = spawns.First();
        partnerComp.PreviousPartner = approacher;

        if (TryComp<InteractionPopupComponent>(approached, out var interactionPopup))
            Spawn(interactionPopup.InteractSuccessSpawn, _transform.GetMapCoordinates(approached));

        // GET HUNGRY GET THIRSTY
        _hunger.ModifyHunger(approacher, -approacher.Comp.HungerPerBirth);
        _hunger.ModifyHunger(approached, -partnerComp.HungerPerBirth);

        _thirst.ModifyThirst(approacher, -approacher.Comp.HungerPerBirth);
        _thirst.ModifyThirst(approached, -partnerComp.HungerPerBirth);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(approached)} (carrier) and {ToPrettyString(approacher)} (partner) successfully bred.");
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
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        // Already gestating.
        if (HasComp<GestatingComponent>(entity.Owner))
            return false;

        // Incapable of gestation.
        if (_gestation.IsUnableToGestate(entity.Owner))
            return false;

        // Too many breedable animals in the area.
        if (AtBreedableCapacity(entity.Owner))
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
    ///     Check if a given entity has too many breedable animals in its range to facilitate breeding.
    /// </summary>
    /// <param name="uid">The entity to check for breedable animals in range.</param>
    /// <returns>Whether or not there are too many breedable animals in the area to keep breeding.</returns>
    public bool AtBreedableCapacity(EntityUid uid)
    {
        // Limit is 0 - that means we're always at capacity.
        if (MaxBreedableAnimalsCount == 0)
            return true;

        // Limit is -1 - that means there is no limit.
        if (MaxBreedableAnimalsCount == -1)
            return false;

        var query = EntityQuery<ImpReproductiveComponent>();
        var breedableCount = query.Count();

        // If there are less total breedable animals than the maximum, then it's not possible for
        // us to be at the limit anyway. This can save us a costly "in range" check.
        if (breedableCount < MaxBreedableAnimalsCount)
            return false;

        // Range is -1 - infinite range, just check the total breedable entity count.
        // We've already checked if it's less, and it's not. Which means we're already at capacity.
        if (BreedableLimitRange == -1)
            return true;

        // Otherwise - get entities in range and check if we have too many for this range.
        var xform = Transform(uid);
        var partners = new HashSet<Entity<ImpReproductiveComponent>>();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, BreedableLimitRange, partners);

        if (partners.Count >= MaxBreedableAnimalsCount)
            return true;

        return false;
    }

    /// <summary>
    ///     Updates a reproductive entity's partner search time with a random duration.
    /// </summary>
    /// <param name="entity">The reproductive entity.</param>
    public void RefreshSearchTime(Entity<ImpReproductiveComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return;

        var newDuration = _random.Next(entity.Comp.MinSearchAttemptInterval, entity.Comp.MaxSearchAttemptInterval);
        entity.Comp.NextSearch = _time.CurTime + newDuration;
    }

    /// <summary>
    ///     Gets whether or not a reproductive entity is ready to search for a new partner.
    /// </summary>
    /// <param name="entity">The reproductive entity.</param>
    /// <returns>Whether or not a reproductive entity is ready to search for a new partner.</returns>
    public bool ReadyToSearch(Entity<ImpReproductiveComponent> entity)
    {
        return _time.CurTime >= entity.Comp.NextSearch;
    }

    /// <summary>
    /// Handles spawning a new mob for us
    /// </summary>
    /// <param name="entity">Entity calling the creation</param>
    /// <param name="toSpawn">What entity will be spawned</param>
    /// <returns>The ID of our new mob! Or nothing if for some reason it didn't spawn.</returns>
    public EntityUid SpawnOnTop(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);
        var newMob = SpawnAtPosition(toSpawn, xform.Coordinates);

        return newMob;
    }
}
