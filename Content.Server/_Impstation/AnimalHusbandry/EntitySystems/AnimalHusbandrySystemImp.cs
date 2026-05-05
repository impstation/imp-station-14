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

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
///     This system handles animal breading, used in conjunction with HTN.
/// </summary>
public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AnimalGestationSystem _gestation = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    public override void Initialize()
    {
        base.Initialize();
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
