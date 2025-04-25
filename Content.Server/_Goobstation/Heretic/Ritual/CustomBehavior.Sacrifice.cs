using Content.Server._Goobstation.Heretic.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Heretic.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Humanoid;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Forensics.Components;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
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
    ///     Should we count only targets?
    /// </summary>
    [DataField] public bool OnlyTargets = false;

    // this is awful but it works so i'm not complaining
    // i'm complaining -kandiyaki
    // IM ALSO COMPLAINING -mq
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly HellWorldSystem _hellworld = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    private IPrototypeManager _proto = default!;
    private IEntityManager _entmanager = default!;
    private List<EntityUid> _uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _entmanager = IoCManager.Resolve<IEntityManager>();


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
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look)) // only humans
                continue;

            if (mobstate.CurrentState == Shared.Mobs.MobState.Dead)
                _uids.Add(look);
        }

        if (_uids.Count < Min)
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

        for (int i = 0; i < Max; i++)
        {
            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(_uids[i]);
            var knowledgeGain = isCommand ? 2f : 1f;

            //get the humanoid appearance component
            if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(_uids[i], out var humanoid))
                return;

            //get the species prototype from that
            if (!_proto.TryIndex(humanoid.Species, out var speciesPrototype))
                return;

            //spawn a clone of the victim
            //this should really use the cloningsystem but i coded this before that existed
            //and it works so i'm not changing it unless it causes issues
            var sacrificialWhiteBoy = args.EntityManager.Spawn(speciesPrototype.Prototype, _transformSystem.GetMapCoordinates(_uids[i]));
            _humanoid.CloneAppearance(_uids[i], sacrificialWhiteBoy);
            //make sure it has the right DNA
            if (args.EntityManager.TryGetComponent<DnaComponent>(_uids[i], out var victimDna))
            {
                if (args.EntityManager.TryGetComponent<BloodstreamComponent>(sacrificialWhiteBoy, out var dummyBlood))
                {
                    //this is copied from BloodstreamSystem's OnDnaGenerated
                    //i hate it
                    if (_solutionContainerSystem.ResolveSolution(sacrificialWhiteBoy, dummyBlood.BloodSolutionName, ref dummyBlood.BloodSolution, out var bloodSolution))
                    {
                        foreach (var reagent in bloodSolution.Contents)
                        {
                            List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                            reagentData.RemoveAll(x => x is DnaData);
                            reagentData.AddRange(_bloodstream.GetEntityBloodData(_uids[i]));
                        }
                    }
                }
            }
            //beat the clone to death. this is just to get matching organs
            if (args.EntityManager.TryGetComponent<DamageableComponent>(_uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>)"Brute";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(sacrificialWhiteBoy, new DamageSpecifier(dmgtype, 1984f), true);
            }

            //send the target to hell world
            _hellworld.AddVictimComponent(_uids[i]);

            //teleport the body to a midround antag spawn spot so it's not just tossed into space
            _hellworld.TeleportRandomly(args, _uids[i]);

            //make sure that my shitty AddVictimComponent thing actually worked before trying to use a mind that isn't there
            if (args.EntityManager.TryGetComponent<HellVictimComponent>(_uids[i], out var hellVictim))
            {
                //i'm so sorry to all of my computer science professors. i've failed you
                if (hellVictim.HasMind)
                {
                    _hellworld.SendToHell(_uids[i], args, speciesPrototype);
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
        _uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }
}
