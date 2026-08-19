using Content.Server.Heretic.EntitySystems;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared._Impstation.Heretic;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Class for handling sacrifices.
/// </summary>
/// <remarks> Marked as virtual because this also is used by ascensions.</remarks>
[Virtual]
public partial class SacrificeBehavior : SharedRitualBehaviorSystem
{
    // Dependencies
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    // Fields
    /// <summary>
    /// Minimum amount of corpses.
    /// </summary>
    [DataField] public float Min = 1;

    /// <summary>
    /// Maximum amount of corpses.
    /// </summary>
    [DataField] public float Max = 1;

    /// <summary>
    /// Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float SacrificePoints = 2f;

    /// <summary>
    /// Points gained on sacrificing a command member.
    /// </summary>
    [DataField] public float CommandSacrificePoints = 3f;

    /// <summary>
    /// The type of damage to do to victims who aren't already dead.
    /// </summary>
    [DataField]
    public DamageSpecifier SacDamage = new()
    {
        DamageDict = new()
        {
            {"Asphyxiation", 100},
        }
    };

    /// <summary>
    /// List of entities being sacrificed.
    /// </summary>
    protected List<EntityUid> Uids = [];

    public override void Initialize()
    {
        base.Initialize();
    }

    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Check if there's even anything on the circle, if there is, add it to a list.
        var lookup = _lookup.GetEntitiesInRange(platform, .75f);
        if (lookup.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ritual-fail-sacrifice"), platform, performer);
            return false;
        }

        foreach (var look in lookup)
        {
            if (!TryComp<MobStateComponent>(look, out var mobstate) // only mobs
            || !HasComp<HumanoidAppearanceComponent>(look) //player races only
            || HasComp<NoSacrificeComponent>(look) //no reusing corpses
            || HasComp<GhoulComponent>(look)) //shouldn't happen because they gib on death but. sanity check
                continue;

            if (mobstate.CurrentState != MobState.Alive)
                Uids.Add(look);

            if (mobstate.CurrentState == MobState.Critical) //if still alive, do enough damage to kill
            {
                _dmg.TryChangeDamage(look, SacDamage, true, origin: performer);
            }
        }

        // If none are dead, say so.
        if (Uids.Count < Min)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ritual-fail-sacrifice-ineligible"), platform, performer);
            return false;
        }

        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // TryDoRitual already checks this, but we need hereticComp later.
        if (!TryComp<HereticComponent>(performer, out var hereticComp))
            return;

        // Now actually sacrifice them
        for (var i = 0; i < Max; i++)
        {
            var isCommand = HasComp<CommandStaffComponent>(Uids[i]);
            var knowledgeGain = isCommand ? CommandSacrificePoints : SacrificePoints;

            //add the component to track the hell adventure
            AddComp<InHellComponent>(Uids[i]);

            //make a hell event and send it
            //idk why i split these up but i'll probably thank myself for it later
            EntityManager.EventBus.RaiseLocalEvent(Uids[i], new HereticBeforeHellEvent());
            EntityManager.EventBus.RaiseLocalEvent(Uids[i], new HereticSendToHellEvent());

            //update the heretic's knowledge
            _heretic.UpdateKnowledge(performer, hereticComp, knowledgeGain);

            // update objectives
            if (_mind.TryGetMind(performer, out var mindId, out var mind))
            {
                // this is godawful dogshit. but it works :)
                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeObjective", out var crewObj)
                && TryComp<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                    crewObjComp.Sacrificed += 1;

                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
                && TryComp<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
                && isCommand)
                    crewHeadObjComp.Sacrificed += 1;
            }
        }

        // Reset the list of uids.
        Uids = [];

        // Update targets.
        EntityManager.EventBus.RaiseLocalEvent(performer, new EventHereticUpdateTargets());

        return;
    }
}
