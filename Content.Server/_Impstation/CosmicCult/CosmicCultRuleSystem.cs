using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Roles;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
using Content.Shared.Damage;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Player;
using Content.Server.EUI;
using Robust.Shared.Random;
using Content.Server.Announcements.Systems;
using Robust.Server.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Parallax;
using Robust.Shared.Map.Components;
using Content.Shared.Temperature.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Objectives.Components;
using Content.Server.Radio.EntitySystems;
using Robust.Server.Player;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Events;

namespace Content.Server._Impstation.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!; // TODO: add logs for Cosmic Cult
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AnnouncerSystem _announce = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> NanoTrasenFactionId = "NanoTrasen";
    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> CosmicFactionId = "CosmicCultFaction";
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");
    public Entity<MonumentComponent> MonumentInGame; // the monument in the current round.
    public int CurrentTier; // current cult tier
    public int TotalCrew; // total connected players
    public int TotalCult; // total cultists
    public int TotalNotCult; // total players that -aren't- cultists
    public int CrewTillNextTier; // players needed to be converted till next monument tier
    public double PercentConverted; // percentage of connected players that are cultists
    public double Tier3Percent; // 40 percent of connected players

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultLeadComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        CurrentTier = 0;
        TotalCrew = 0;
        TotalCult = 0;
        TotalNotCult = 0;
        CrewTillNextTier = 777;
        PercentConverted = 0;
        Tier3Percent = 50;
        Log.Debug($"Cosmic cult data reset.");
    }

    private void OnAntagSelect(Entity<CosmicCultRuleComponent> uid, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, uid);
    }

    public void UpdateCultData(Entity<MonumentComponent> uid)
    {
        TotalCrew = _antag.GetTotalPlayerCount(_playerMan.Sessions);
        TotalNotCult = TotalCrew - TotalCult;
        PercentConverted = Math.Round((double)(100 * TotalCult) / TotalCrew);
        Tier3Percent = Math.Round((double)25 / 100 * 40); // total players divided by 100 multiplied by 40 to get 40% of current pop. //TODO: VALUE 25 must be replaced with TOTALCREW.
        switch (CurrentTier)
        {
            case 1:
                CrewTillNextTier = Convert.ToInt16(Tier3Percent / 2) - TotalCult;
                break;
            case 2:
                CrewTillNextTier = Convert.ToInt16(Tier3Percent) - TotalCult;
                break;
            default:
                break;
        }
        Log.Debug($"DEBUG: {Tier3Percent} crew for Tier 3. {Tier3Percent /2} crew for Tier 2. {CrewTillNextTier} crew to convert till the next tier"); //todo remove
        Log.Debug($"DEBUG: {TotalCrew} session(s), {TotalCult} cultist(s), {TotalNotCult} non-cult, {PercentConverted}% of the crew has been converted"); //todo remove
    }

    #region Cult Tiers
    public void CultTier1(Entity<MonumentComponent> uid)
    {
        CurrentTier = 1;
        MonumentInGame = uid; //Since there's only one Monument per round, let's store its UID for the rest of the round.
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
        {
            objectiveComp.Tier = 1;
        }
    }
    public void CultTier2()
    {
        CurrentTier = 2;
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier2-progress"), sender, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/tier2.ogg", Filter.Broadcast(), false, AudioParams.Default);
        for (int i = 0; i < _rand.Next(8, 16); i++)
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
                Spawn("CosmicMalignRift", coords);
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
        {
            objectiveComp.Tier = 2;
        }
    }
    public void CultTier3()
    {
        CurrentTier = 3;
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var _))
        {
            EnsureComp<CosmicStarMarkComponent>(cultist);
            RemComp<BarotraumaComponent>(cultist);
            RemComp<TemperatureSpeedComponent>(cultist);
            RemComp<RespiratorComponent>(cultist);
        }
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var map = _transform.GetMapId(MonumentInGame.Owner.ToCoordinates());
        var mapData = _map.GetMap(map);
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier3-progress"), sender, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/tier3.ogg", Filter.Broadcast(), false, AudioParams.Default);
        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "CosmicFinaleParallax";
        Dirty(mapData, parallax);
        EnsureComp<MapLightComponent>(mapData, out var mapLight);
        mapLight.AmbientLightColor = Color.FromHex("#210746");
        Dirty(mapData, mapLight);
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
        {
            objectiveComp.Tier = 3;
        }
    }
    #endregion

    #region The Monument




    #endregion

    #region Con & Deconversion
    public void TryStartCult(EntityUid uid, Entity<CosmicCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);
        if (cosmicRole is not null)
        {
            EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
            Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
        }

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        EnsureComp<CosmicCultComponent>(uid);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = new() { "CosmicRadio" };
        transmitter.Channels = new() { "CosmicRadio" };

        _npcFaction.RemoveFaction(uid, NanoTrasenFactionId);
        _npcFaction.AddFaction(uid, CosmicFactionId);
        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicRoundStartEui(), session);
        }
        TotalCult++;
    }

    public void CosmicConversion(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);
        if (cosmicRole is not null)
        {
            EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
            Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
        }
        _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        EnsureComp<CosmicCultComponent>(uid);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = new() { "CosmicRadio" };
        transmitter.Channels = new() { "CosmicRadio" };

        _npcFaction.RemoveFaction((uid, null), NanoTrasenFactionId);
        _npcFaction.AddFaction((uid, null), CosmicFactionId);

        _mind.TryAddObjective(mindId, mind, "CosmicFinalityObjective");
        _mind.TryAddObjective(mindId, mind, "CosmicMonumentObjective");
        _mind.TryAddObjective(mindId, mind, "CosmicEntropyObjective");
        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicConvertedEui(), session);
        }
        TotalCult++;
    }
    #endregion

    private void DebugFunction(EntityUid uid, CosmicCultLeadComponent comp, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
    }

}
