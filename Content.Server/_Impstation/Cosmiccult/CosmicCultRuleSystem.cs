using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking;
using Content.Server._Impstation.Cosmiccult;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Server.Roles;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Cuffs.Components;

namespace Content.Server._Impstation.Cosmiccult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public readonly ProtoId<NpcFactionPrototype> CosmicCultFactionId = "CosmicCultFaction";
    public readonly ProtoId<NpcFactionPrototype> CosmicCultPrototypeId = "CosmicCult";
    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/cosmiccult_start.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<CosmicCultLeadComponent, AfterFlashedEvent>(OnPostFlash);

    }

    /// FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING
    private void OnAntagSelect(Entity<CosmicCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, ent.Comp);
    }

    public bool TryStartCult(EntityUid target, CosmicCultRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        // briefing
        if (TryComp<MetaDataComponent>(target, out var metaData))
        {
            var briefingShort = Loc.GetString("objective-cosmiccult-description", ("name", metaData?.EntityName ?? "Unknown"));

            _antag.SendBriefing(target, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.MediumTurquoise, BriefingSound);
            _antag.SendBriefing(target, Loc.GetString("cosmiccult-role-short-briefing"), Color.DarkOrchid, null);

            if (_role.MindHasRole<CosmicCultRoleComponent>(mindId, out var rbc))
            {
                EnsureComp<RoleBriefingComponent>(rbc.Value.Owner);
                Comp<RoleBriefingComponent>(rbc.Value.Owner).Briefing = briefingShort;
            }
            else
                _role.MindAddRole(mindId, "CosmicCultRole", mind, true);
        }

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, CosmicCultFactionId);

        EnsureComp<CosmicCultComponent>(target);

        rule.Minds.Add(mindId);

        foreach (var objective in rule.Objectives)
            _mind.TryAddObjective(mindId, mind, objective);

        return true;
    }
    /// FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING

    /// <summary>
    /// Called when a Cosmic Cult Lead uses a flash in melee to convert somebody else.
    /// </summary>
    private void OnPostFlash(EntityUid uid, CosmicCultLeadComponent comp, ref AfterFlashedEvent ev)
    {
        var alwaysConvertible = HasComp<AlwaysCosmicCultConvertibleComponent>(ev.Target);

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<CosmicCultComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidAppearanceComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, CosmicCultFactionId);
        var cosmiccultComp = EnsureComp<CosmicCultComponent>(ev.Target);

        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Cosmic Cultist!");

            if (_mind.TryGetMind(ev.User.Value, out var cosmiccultMindId, out _))
            {
                if (_role.MindHasRole<CosmicCultRoleComponent>(cosmiccultMindId, out var role))
                    role.Value.Comp2.ConvertedCount++;
            }
        }

        if (mindId == default || !_role.MindHasRole<CosmicCultRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "CosmicCultRole");
        }

        if (mind?.Session != null)
        {
            _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.MediumTurquoise, BriefingSound);
            _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-short-briefing"), Color.DarkOrchid, null);
        }

    }



    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    // private bool CheckRevsLose()
    // {
    //     var stunTime = TimeSpan.FromSeconds(4);
    //     var headRevList = new List<EntityUid>();

    //     var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
    //     while (headRevs.MoveNext(out var uid, out _, out _))
    //     {
    //         headRevList.Add(uid);
    //     }

    //     // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
    //     // Cuffing Head Revs is not enough - they must be killed.
    //     if (IsGroupDetainedOrDead(headRevList, false, false))
    //     {
    //         var rev = AllEntityQuery<RevolutionaryComponent, MindContainerComponent>();
    //         while (rev.MoveNext(out var uid, out _, out var mc))
    //         {
    //             if (HasComp<HeadRevolutionaryComponent>(uid))
    //                 continue;

    //             _npcFaction.RemoveFaction(uid, CosmicCultFactionId);
    //             _stun.TryParalyze(uid, stunTime, true);
    //             RemCompDeferred<CosmicCultComponent>(uid);
    //             _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Entity(uid, EntityManager))), uid);
    //             _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

    //             if (!_mind.TryGetMind(uid, out var mindId, out _, mc))
    //                 continue;

    //             // remove their antag role
    //             _role.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);

    //             // make it very obvious to the rev they've been deconverted since
    //             // they may not see the popup due to antag and/or new player tunnel vision
    //             if (_mind.TryGetSession(mindId, out var session))
    //                 _euiMan.OpenEui(new DeconvertedEui(), session);
    //         }
    //         return true;
    //     }

    //     return false;
    // }

    private static readonly string[] Outcomes =
    {
        // revs survived and heads survived... how
        "rev-reverse-stalemate",
        // revs won and heads died
        "rev-won",
        // revs lost and heads survived
        "rev-lost",
        // revs lost and heads died
        "rev-stalemate"
    };
}
