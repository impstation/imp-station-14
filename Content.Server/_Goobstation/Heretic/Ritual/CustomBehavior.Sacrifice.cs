using Content.Shared.Heretic.Prototypes;
using Content.Shared.Changeling;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Revolutionary.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Humanoid;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Server.GameObjects;
using System;
using Robust.Shared.Random;
using System.Linq;



namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     gibs it and gives the heretic knowledge points.
/// </summary>
// these classes should be lead out and shot
[Virtual] public partial class RitualSacrificeBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField] public float Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField] public float Max = 1;

    /// <summary>
    ///     Should we count only targets?
    /// </summary>
    [DataField] public bool OnlyTargets = false;

    // this is awful but it works so i'm not complaining
    protected SharedMindSystem _mind = default!;
    protected HereticSystem _heretic = default!;
    protected SharedTransformSystem _xform = default!;

    protected DamageableSystem _damage = default!;
    protected EntityLookupSystem _lookup = default!;
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected IRobustRandom _random = default!;
    private HumanoidAppearanceSystem _humanoid = default!;
    private TransformSystem _transformSystem = default!;
    [Dependency] protected IEntityManager _mapsys = default!;


    protected List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        //it was like this when i got here -kandiyaki
        _mind = args.EntityManager.System<SharedMindSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _damage = args.EntityManager.System<DamageableSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _transformSystem = args.EntityManager.System<TransformSystem>();
        _humanoid = args.EntityManager.System<HumanoidAppearanceSystem>();



        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);
        if (lookup.Count == 0 || lookup == null)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look) // only humans
            || (OnlyTargets && !hereticComp.SacrificeTargets.Contains(args.EntityManager.GetNetEntity(look)))) // only targets
                continue;

            if (mobstate.CurrentState == Shared.Mobs.MobState.Dead)
                uids.Add(look);
        }

        if (uids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        

        for (int i = 0; i < Max; i++)
        {

            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(uids[i]);
            var knowledgeGain = isCommand ? 2f : 1f;

            //get the humanoid appearance component
            if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(uids[i], out var humanoid))
                return;

            //get the species prototype from that
            if (!_proto.TryIndex(humanoid.Species, out var speciesPrototype))
                return;

            //spawn a clone of the victim 
            var sacrificialWhiteBoy = args.EntityManager.Spawn(speciesPrototype.Prototype, _transformSystem.GetMapCoordinates(uids[i]));
            _humanoid.CloneAppearance(uids[i], sacrificialWhiteBoy);

            //beat them to death
            if (args.EntityManager.TryGetComponent<DamageableComponent>(uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>) "Brute";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(sacrificialWhiteBoy, new DamageSpecifier(dmgtype, 1984f), true);
            }

            //TODO: send the target to hell world here


            //update the heretic's knowledge
            if (args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
                _heretic.UpdateKnowledge(args.Performer, hereticComp, knowledgeGain);

            // update objectives
            if (_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            {
                // this is godawful dogshit. but it works :)
                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeObjective", out var crewObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                    crewObjComp.Sacrificed += 1;

                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
                && isCommand)
                    crewHeadObjComp.Sacrificed += 1;
            }
        }

        // reset it because it refuses to work otherwise.
        uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }

    //ported from funkystation
    private void TeleportRandomly(TransformComponent transform, RitualData args, EntityUid uid) // start le teleporting loop -space
    {
        var maxrandomtp = 40; // this is how many attempts it will try before breaking the loop -space
        var maxrandomradius = 40; // this is the max range it will do -space


        if (!args.EntityManager.TryGetComponent<TransformComponent>(uid, out var xform))
            return;
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(maxrandomradius));
        for (var i = 0; i < maxrandomtp; i++) //start of the loop -space
        {
            var randVector = _random.NextVector2(maxrandomradius);
            newCoords = coords.Offset(randVector);
            if (!args.EntityManager.TryGetComponent<TransformComponent>(uid, out var trans))
                continue;
            if (trans.GridUid != null && !_lookup.GetEntitiesIntersecting(newCoords.ToMap(_mapsys, _xform), LookupFlags.Static).Any()) // if they're not in space and not in wall, it will choose these coords and end the loop -space
            {
                break;
            }
        }

        _xform.SetCoordinates(uid, newCoords);
    }
}
