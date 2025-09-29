using Content.Server._Goobstation.Heretic.EntitySystems;
using Content.Server.Cloning;
using Content.Server.Heretic.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Cloning;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     gibs it and gives the heretic knowledge points.
/// </summary>
// these classes should be lead out and shot
[Virtual]
public partial class RitualSacrificeBehavior : RitualCustomBehavior
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
    ///     Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float SacrificePoints = 2f;

    /// <summary>
    ///     Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float CommandSacrificePoints = 3f;

    [DataField] public ProtoId<CloningSettingsPrototype> Settings = "HellClone";

    // this is awful but it works so i'm not complaining
    // i'm complaining -kandiyaki
    // IM ALSO COMPLAINING -mq
    // im mad. -honeyed
    private EntityLookupSystem _lookup = default!;
    private HellWorldSystem _hellworld = default!;
    private HereticSystem _heretic = default!;
    private SharedMindSystem _mind = default!;
    private TransformSystem _transformSystem = default!;
    private CloningSystem _cloning = default!;
    private SharedBodySystem _body = default!;

    protected List<EntityUid> Uids = [];

    public override bool Execute(RitualData args, out string? outstr)
    {
        // this fucking sucks -mq
        // why is this like this -honeyed
        _hellworld = args.EntityManager.System<HellWorldSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _mind = args.EntityManager.System<SharedMindSystem>();
        _transformSystem = args.EntityManager.System<TransformSystem>();
        _cloning = args.EntityManager.System<CloningSystem>();
        _body = args.EntityManager.System<SharedBodySystem>();

        //if the performer isn't a heretic, stop
        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out _))
        {
            outstr = string.Empty;
            return false;
        }

        //get all entities in range of the circle
        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);
        if (lookup.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look) //player races only
            || args.EntityManager.HasComponent<HellVictimComponent>(look) //no reusing corpses
            || args.EntityManager.HasComponent<GhoulComponent>(look)) //shouldn't happen because they gib on death but. sanity check
                continue;

            if (mobstate.CurrentState != MobState.Alive)
                Uids.Add(look);
        }

        //if none are dead, say so
        if (Uids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    //this does way too much
    public override void Finalize(RitualData args)
    {

        for (var i = 0; i < Max; i++)
        {
            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(Uids[i]);
            var knowledgeGain = isCommand ? CommandSacrificePoints : SacrificePoints;

            //spawn a clone of the victim
            _cloning.TryCloning(Uids[i], _transformSystem.GetMapCoordinates(Uids[i]), Settings, out var clone);

            //gib clone to get matching organs.
            if (clone != null)
                _body.GibBody(clone.Value, true);

            //send the target to hell world
            _hellworld.AddVictimComponent(Uids[i]);

            //teleport the body to a midround antag spawn spot so it's not just tossed into space
            _hellworld.TeleportRandomly(args, Uids[i]);

            //make sure that my shitty AddVictimComponent thing actually worked before trying to use a mind that isn't there
            if (args.EntityManager.TryGetComponent<HellVictimComponent>(Uids[i], out var hellVictim))
            {
                //i'm so sorry to all of my computer science professors. i've failed you
                if (hellVictim.HasMind)
                {
                    _hellworld.SendToHell(Uids[i], args);
                }
            }

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
        Uids = [];
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }
}
