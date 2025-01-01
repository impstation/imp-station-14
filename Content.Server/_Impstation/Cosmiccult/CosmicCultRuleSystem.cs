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
using Content.Server.Radio.Components;
using Content.Shared.Heretic;
using Robust.Shared.Player;
using Content.Server.Antag.Components;
using System.Linq;

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
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        // SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCosmicCultComponentInit);
        // SubscribeLocalEvent<CosmicCultComponent, ComponentRemove>(OnCosmicCultComponentRemoved);
        // SubscribeLocalEvent<CosmicCultComponent, MobStateChangedEvent>(OnCosmicCultistStateChanged);
        // SubscribeLocalEvent<CosmicCultRoleComponent, GetBriefingEvent>(OnGetBriefing);

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
            var briefingShort = Loc.GetString("objective-cosmiccult-charactermenu", ("name", metaData?.EntityName ?? "Unknown"));

            _antag.SendBriefing(target, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
            _antag.SendBriefing(target, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

            if (_role.MindHasRole<CosmicCultRoleComponent>(mindId, out var rbc))
            {
                EnsureComp<RoleBriefingComponent>(rbc.Value.Owner);
                Comp<RoleBriefingComponent>(rbc.Value.Owner).Briefing = briefingShort;
            }
            else
                _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        }

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, CosmicCultFactionId);

        EnsureComp<CosmicCultComponent>(target);
        var reciever = EnsureComp<IntrinsicRadioReceiverComponent>(target);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);
        var radio = EnsureComp<ActiveRadioComponent>(target);
        radio.Channels = new() { "CosmicRadio" };
        transmitter.Channels = new() { "CosmicRadio" };

        return true;
    }
    /// FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING FRANKENSTEINED HERETIC CODE FOR BRIEFING


    /// <summary>
    /// Called when we want to convert into the cult post-roundstart.
    /// </summary>
    public void Convert(EntityUid target)
    {
        if (!TryComp(target, out ActorComponent? actor))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out var ruleUid, out _, out _, out _))
        {
            if (!TryComp(ruleUid, out AntagSelectionComponent? antagSelection))
                continue;

            var antagSelectionEnt = (ruleUid, antagSelection);
            if (!_antag.TryGetNextAvailableDefinition(antagSelectionEnt, out var def))
                def = antagSelection.Definitions.Last();

            _antag.MakeAntag(antagSelectionEnt, actor.PlayerSession, def.Value, null);
        }
    }

    // private void OnPostFlash(EntityUid uid, CosmicCultLeadComponent comp, ref AfterFlashedEvent ev)
    // {
    //     var alwaysConvertible = HasComp<AlwaysCosmicCultConvertibleComponent>(ev.Target);

    //     if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
    //         return;

    //     if (HasComp<CosmicCultComponent>(ev.Target) ||
    //         HasComp<MindShieldComponent>(ev.Target) ||
    //         HasComp<HereticComponent>(ev.Target) || ///LET'S MAKE SURE TO CATCH HERETICS SO WE CAN'T CONVERT THESE
    //         !HasComp<HumanoidAppearanceComponent>(ev.Target) &&
    //         !alwaysConvertible ||
    //         !_mobState.IsAlive(ev.Target) ||
    //         HasComp<ZombieComponent>(ev.Target))
    //     {
    //         return;
    //     }

    //     _npcFaction.AddFaction(ev.Target, CosmicCultFactionId);
    //     var cosmiccultComp = EnsureComp<CosmicCultComponent>(ev.Target);

    //     if (ev.User != null)
    //     {
    //         _adminLogManager.Add(LogType.Mind,
    //             LogImpact.Medium,
    //             $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Cosmic Cultist!");

    //         if (_mind.TryGetMind(ev.User.Value, out var cosmiccultMindId, out _))
    //         {
    //             if (_role.MindHasRole<CosmicCultRoleComponent>(cosmiccultMindId, out var role))
    //                 role.Value.Comp2.ConvertedCount++;
    //         }
    //     }

    //     if (mindId == default || !_role.MindHasRole<CosmicCultRoleComponent>(mindId))
    //     {
    //         _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
    //     }

    //     if (mind?.Session != null)
    //     {
    //         _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
    //         _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);
    //         // EnsureComp<RoleBriefingComponent>(ev.Target);
    //         // Comp<RoleBriefingComponent>(ev.Target).Briefing = Loc.GetString("objective-cosmiccult-description", ("name", mind.CharacterName ?? "Unknown"));
    //     }

    // }



}
