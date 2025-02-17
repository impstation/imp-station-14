using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Roles;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
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
using Robust.Server.Player;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Shared.Administration.Logs;
using Content.Server.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Linq;
using Content.Server.Shuttles.Systems;

namespace Content.Server._Impstation.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly ISharedAdminLogManager _log = default!; //TODO: Admin logging, probably.
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AnnouncerSystem _announce = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> NanoTrasenFactionId = "NanoTrasen";
    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> CosmicFactionId = "CosmicCultFaction";
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");
    public readonly SoundSpecifier DeconvertSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_deconvert.ogg");
    public Entity<MonumentComponent> MonumentInGame; // the monument in the current round.
    public int CurrentTier; // current cult tier
    public int TotalCrew; // total connected players
    public int TotalCult; // total cultists
    public int TotalNotCult; // total players that -aren't- cultists
    public int TotalEntropy; // total entropy in the monument
    public int CrewTillNextTier; // players needed to be converted till next monument tier
    public float CurrentProgress; // percent of progress towards the next tier
    public float TargetProgress; // current tier's progress target
    public double PercentConverted; // percentage of connected players that are cultists
    public double Tier3Percent; // 40 percent of connected players
    public int EntropySiphoned; // the total entropy siphoned by the cult.
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnRoundStart(RoundStartingEvent ev) // Reset the cult data to defaults.
    {
        EntropySiphoned = 0;
        CurrentTier = 0;
        TotalCrew = 0;
        TotalCult = 0;
        TotalNotCult = 0;
        TotalEntropy = 0;
        CrewTillNextTier = 40;
        PercentConverted = 0;
        CurrentProgress = 0.001f;
        TargetProgress = 80;
        Tier3Percent = 40;
    }

    private void OnAntagSelect(Entity<CosmicCultRuleComponent> uid, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, uid);
    }

    #region Round & Objectives

    private void SetWinType(Entity<CosmicCultRuleComponent> uid, WinType type)
    {
        if (uid.Comp.WinLocked == true)
            return;
        uid.Comp.WinType = type;

        if (type == WinType.CultComplete || type == WinType.CrewComplete || type == WinType.CultMajor)
            uid.Comp.WinLocked = true;
    }
    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New is not GameRunLevel.PostRound)
            return;

        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out var uid, out _, out var cultRule, out _))
        {
            OnRoundEnd((uid, cultRule));
        }
    }
    private void OnRoundEnd(Entity<CosmicCultRuleComponent> uid)
    {
        var tier = CurrentTier;
        var leaderAlive = false;
        var centcomm = _emergency.GetCentcommMaps();
        var wrapup = AllEntityQuery<CosmicCultComponent, TransformComponent>();
        while (wrapup.MoveNext(out var cultist, out _, out var cultistLocation))
        {
            if (cultistLocation.MapUid != null && centcomm.Contains(cultistLocation.MapUid.Value))
            {
                if (HasComp<CosmicCultLeadComponent>(cultist))
                    leaderAlive = true;
            }
        }
        if (tier < 3 && leaderAlive)
            SetWinType(uid, WinType.Neutral);
        var monument = AllEntityQuery<CosmicFinaleComponent>();
        while (monument.MoveNext(out _, out var comp))
        {
            if (comp.FinaleActive || comp.BufferComplete || comp.FinaleReady || comp.OnCooldown)
            {
                SetWinType(uid, WinType.CultMajor);
            }
            else if (tier == 3)
            {
                SetWinType(uid, WinType.CultMinor);
            }
        }

        var cultists = EntityQuery<CosmicCultComponent, MobStateComponent>(true);
        var cultistsAlive = cultists
            .Any(op => op.Item2.CurrentState == MobState.Alive && op.Item1.Running);
        if (cultistsAlive)
            return; // There's still cultists alive! Return.
        SetWinType(uid, WinType.CrewMajor); // There's still cultists registered, but they're all dead. That's all, folks.

        if (TotalCult == 0)
            SetWinType(uid, WinType.CrewComplete); // A Complete victory locks in the wincondition, so we don't need to add a return past this.
    }
    protected override void AppendRoundEndText(EntityUid uid,
        CosmicCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"cosmiccult-roundend-{component.WinType.ToString().ToLower()}");
        args.AddLine(winText);
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultist-count", ("initialCount", TotalCult)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultpop-count", ("count", PercentConverted)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-entropy-count", ("count", EntropySiphoned)));
    }
    public void IncrementCultObjectiveEntropy(Entity<CosmicCultComponent> uid)
    {
        EntropySiphoned += uid.Comp.CosmicSiphonQuantity;
        var query = EntityQueryEnumerator<CosmicEntropyConditionComponent>();
        while (query.MoveNext(out var _, out var entropyComp))
        {
            entropyComp.Siphoned = EntropySiphoned;
        }
    }
    #endregion

    #region Monument
    public void UpdateMonumentAppearance(Entity<MonumentComponent> uid, bool tierUp) // this is kinda awful, but it works, and i've seen worse. improve it at thine leisure
    {
        if (!TryComp<CosmicFinaleComponent>(uid, out var finaleComp))
            return;
        _appearance.SetData(uid, MonumentVisuals.Monument, CurrentTier);
        if (CurrentTier == 3) _appearance.SetData(uid, MonumentVisuals.Tier3, true);
        else if (CurrentTier == 2) _appearance.SetData(uid, MonumentVisuals.Tier3, false);
        if (tierUp)
        {
            var transformComp = EnsureComp<MonumentTransformingComponent>(uid);
            transformComp.EndTime = _timing.CurTime + uid.Comp.TransformTime;
            _appearance.SetData(uid, MonumentVisuals.Transforming, true);
        }
        if (finaleComp.FinaleReady || finaleComp.FinaleActive) _appearance.SetData(uid, MonumentVisuals.FinaleReached, true);
    }
    public void UpdateCultData(Entity<MonumentComponent> uid)
    {
        if (uid.Comp == null || !TryComp<CosmicFinaleComponent>(uid, out var finaleComp))
            return;
        TotalCrew = _antag.GetTotalPlayerCount(_playerMan.Sessions);
        TotalNotCult = TotalCrew - TotalCult;
        PercentConverted = Math.Round((double)(100 * TotalCult) / TotalCrew);
        Tier3Percent = Math.Round((double)25 / 100 * 40); // 40% of current pop //TODO: VALUE 25 must be replaced with TOTALCREW.
        if (CurrentTier == 1)
        {
            CrewTillNextTier = Convert.ToInt16(Tier3Percent / 2) - TotalCult;
            TargetProgress = Convert.ToInt16(Tier3Percent / 2 * 3);
        }
        else if (CurrentTier == 2)
        {
            CrewTillNextTier = Convert.ToInt16(Tier3Percent) - TotalCult;
            TargetProgress = Convert.ToInt16(Tier3Percent * 3);
        }
        if (CurrentTier == 3) TargetProgress = Convert.ToInt16(Tier3Percent) * 3 + 20;

        CurrentProgress = TotalEntropy + TotalCult * 3;

        if (CurrentTier < 3) uid.Comp.CrewToConvertNextStage = Convert.ToInt16(Math.Ceiling(Convert.ToDouble((TargetProgress - CurrentProgress) / 3)));
        uid.Comp.EntropyUntilNextStage = Convert.ToInt16(TargetProgress) - Convert.ToInt16(CurrentProgress);

        uid.Comp.PercentageComplete = CurrentProgress / TargetProgress * 100;
        Math.Round(uid.Comp.PercentageComplete);
        if (CurrentProgress >= TargetProgress && CurrentTier == 3 && !finaleComp.FinaleActive && !finaleComp.FinaleReady) FinaleReady(uid, finaleComp);
        else if (finaleComp.FinaleReady || finaleComp.FinaleActive) { uid.Comp.CrewToConvertNextStage = 0; uid.Comp.EntropyUntilNextStage = 0; uid.Comp.PercentageComplete = 100; }
        else if (CurrentProgress >= TargetProgress && CurrentTier == 2) MonumentTier3(uid);
        else if (CurrentProgress >= TargetProgress && CurrentTier == 1) MonumentTier2(uid);
        UpdateMonumentAppearance(uid, false);
    }
    public void MonumentTier1(Entity<MonumentComponent> uid)
    {
        CurrentTier = 1;
        UpdateMonumentAppearance(uid, false);
        MonumentInGame = uid; //Since there's only one Monument per round, let's store its UID for the rest of the round
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
        {
            objectiveComp.Tier = 1;
        }
    }
    private void MonumentTier2(Entity<MonumentComponent> uid)
    {
        uid.Comp.PercentageComplete = 50;
        CurrentTier = 2;
        UpdateMonumentAppearance(uid, true);
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out _, out var cultComp))
        {
            cultComp.UnlockedInfluences.Add("InfluenceForceIngress");
            cultComp.EntropyBudget += Convert.ToInt16(Math.Floor(Math.Round((double)25 / 100 * 4))); // pity system. 4% of the playercount worth of entropy on tier up //TODO: VALUE 25 must be replaced with TOTALCREW.
        }
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier2-progress"), sender, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/tier2.ogg", Filter.Broadcast(), false, AudioParams.Default);
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
        {
            objectiveComp.Tier = 2;
        }
        for (int i = 0; i < _rand.Next(Convert.ToInt16(Math.Floor(Math.Round((double)25 / 100 * 25)))); i++) // spawn # malign rifts equal to 25% of the playercount //TODO: VALUE 25 must be replaced with TOTALCREW.
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
                Spawn("CosmicMalignRift", coords);
    }
    private void MonumentTier3(Entity<MonumentComponent> uid)
    {
        uid.Comp.PercentageComplete = 0;
        CurrentTier = 3;
        _visibility.SetLayer(uid.Owner, 1, true);
        UpdateMonumentAppearance(uid, true);
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {
            EnsureComp<CosmicStarMarkComponent>(cultist);
            EnsureComp<PressureImmunityComponent>(cultist);
            RemComp<TemperatureSpeedComponent>(cultist);
            RemComp<RespiratorComponent>(cultist);
            cultComp.UnlockedInfluences.Add("InfluenceAstralLash");
            cultComp.UnlockedInfluences.Add("InfluenceNullGlare");
            cultComp.EntropyBudget += Convert.ToInt16(Math.Floor(Math.Round((double)TotalCrew / 100 * 4))); //pity system. 4% of the playercount worth of entropy on tier up //TODO: VALUE 25 must be replaced with TOTALCREW.
        }
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var mapData = _map.GetMap(_transform.GetMapId(uid.Owner.ToCoordinates()));
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
    private void FinaleReady(Entity<MonumentComponent> uid, CosmicFinaleComponent finaleComp)
    {
        if (TryComp<CosmicCorruptingComponent>(uid, out var comp)) comp.Enabled = true;
        finaleComp.FinaleReady = true;
        uid.Comp.Enabled = false;
        _popup.PopupCoordinates(Loc.GetString("cosmiccult-finale-ready"), Transform(uid).Coordinates, PopupType.Large);
    }
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
        var query = QueryActiveRules();
        while (query.MoveNext(out var ruleUid, out _, out var cosmicGamerule, out _))
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

            var cultComp = EnsureComp<CosmicCultComponent>(uid);
            EnsureComp<IntrinsicRadioReceiverComponent>(uid);

            if (CurrentTier == 3)
            {
                cultComp.EntropyBudget = 12; //TODO: tier pity balance
                EnsureComp<CosmicStarMarkComponent>(uid);
                EnsureComp<PressureImmunityComponent>(uid);
                EnsureComp<TemperatureImmunityComponent>(uid);
                RemComp<RespiratorComponent>(uid);
            }
            else if (CurrentTier == 2) cultComp.EntropyBudget = 6; //TODO: tier pity balance
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
            cosmicGamerule.Cultists.Add(uid);
            UpdateCultData(MonumentInGame);
        }
    }
    private void OnComponentRemove(Entity<CosmicCultComponent> uid, ref ComponentRemove args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var ruleUid, out _, out var cosmicGamerule, out _))
        {
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
            foreach (var actionEnt in uid.Comp.ActionEntities) _actions.RemoveAction(actionEnt);

            RemComp<ActiveRadioComponent>(uid); // TODO: clean up components better. Wow this is easy to read but surely this can be done tidier.
            RemComp<IntrinsicRadioReceiverComponent>(uid);
            RemComp<IntrinsicRadioTransmitterComponent>(uid);
            if (HasComp<CosmicCultLeadComponent>(uid))
                RemComp<CosmicCultLeadComponent>(uid);
            if (CurrentTier == 3 || uid.Comp.CosmicEmpowered)
            {
                RemComp<PressureImmunityComponent>(uid);
                RemComp<TemperatureImmunityComponent>(uid);
            }
            if (CurrentTier == 3) RemComp<CosmicStarMarkComponent>(uid);

            _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), DeconvertSound);
            _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);

            if (!_mind.TryGetMind(uid, out var mindId, out _) || !TryComp<MindComponent>(mindId, out var mindComp))
                return;

            _mind.ClearObjectives(mindId, mindComp); // LOAD-BEARING #imp function to remove all of someone's objectives, courtesy of TCRGDev(Github)
            _role.MindTryRemoveRole<CosmicCultRoleComponent>(mindId);
            _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
            if (_mind.TryGetSession(mindId, out var session))
            {
                _euiMan.OpenEui(new CosmicDeconvertedEui(), session);
            }
            TotalCult--;
            cosmicGamerule.Cultists.Remove(uid);
            UpdateCultData(MonumentInGame);
        }
    }
    #endregion
}
