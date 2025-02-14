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
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Server.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultComponent, ComponentShutdown>(OnShutdownCultist);
        SubscribeLocalEvent<CosmicCultLeadComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.
    }

    private void OnRoundStart(RoundStartingEvent ev) // Reset the cult data to defaults.
    {
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
        Log.Debug($"Cleaned up Cosmic Cult data.");
    }

    private void OnAntagSelect(Entity<CosmicCultRuleComponent> uid, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, uid);
    }
    #region Monument
    public void UpdateMonumentAppearance(Entity<MonumentComponent> uid, bool tierUp) // this is fucking awful in its current setup, but nothing else seems to work. Fuck
    {
        _appearance.SetData(uid, MonumentVisuals.Monument, CurrentTier);
        if (CurrentTier == 3) _appearance.SetData(uid, MonumentVisuals.Tier3, true);
        else if (CurrentTier == 2) _appearance.SetData(uid, MonumentVisuals.Tier3, false);
        if (tierUp)
        {
            var transformComp = EnsureComp<MonumentTransformingComponent>(uid);
            transformComp.EndTime = _timing.CurTime + uid.Comp.TransformTime;
            _appearance.SetData(uid, MonumentVisuals.Transforming, true);
        }
        if (uid.Comp.FinaleReady) _appearance.SetData(uid, MonumentVisuals.FinaleReached, true);
    }
    public void UpdateCultData(Entity<MonumentComponent> uid)
    {
        if (uid.Comp == null)
            return;
        TotalCrew = _antag.GetTotalPlayerCount(_playerMan.Sessions);
        TotalNotCult = TotalCrew - TotalCult;
        TotalEntropy = uid.Comp.TotalEntropy;
        PercentConverted = Math.Round((double)(100 * TotalCult) / TotalCrew);
        Tier3Percent = Math.Round((double)25 / 100 * 40); // total players divided by 100 multiplied by 40 to get 40% of current pop. //TODO: VALUE 25 must be replaced with TOTALCREW.
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
        else uid.Comp.CrewToConvertNextStage = 0;
        uid.Comp.EntropyUntilNextStage = Convert.ToInt16(TargetProgress) - Convert.ToInt16(CurrentProgress);

        uid.Comp.PercentageComplete = CurrentProgress / TargetProgress * 100;
        Math.Round(uid.Comp.PercentageComplete);
        if (CurrentProgress >= TargetProgress && CurrentTier == 3) Finale(uid);
        else if (CurrentProgress >= TargetProgress && CurrentTier == 2) MonumentTier3(uid);
        else if (CurrentProgress >= TargetProgress && CurrentTier == 1) MonumentTier2(uid);
        Log.Debug($"DEBUG: {Tier3Percent} crew for Tier 3. {Tier3Percent / 2} crew for Tier 2. {CrewTillNextTier} crew to convert till the next tier"); //todo remove
        Log.Debug($"DEBUG: {TotalCrew} session(s), {TotalCult} cultist(s), {TotalNotCult} non-cult, {PercentConverted}% of the crew has been converted"); //todo remove
        UpdateMonumentAppearance(uid, false);
        Dirty(uid);
    }
    public void MonumentTier1(Entity<MonumentComponent> uid)
    {
        CurrentTier = 1;
        UpdateMonumentAppearance(uid, false);
        MonumentInGame = uid; //Since there's only one Monument per round, let's store its UID for the rest of the round.
        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
        {
            objectiveComp.Tier = 1;
        }
    }
    public void MonumentTier2(Entity<MonumentComponent> uid)
    {
        uid.Comp.PercentageComplete = 50;
        CurrentTier = 2;
        uid.Comp.UnlockedInfluences.Add("InfluenceForceIngress");
        UpdateMonumentAppearance(uid, true);
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
    public void MonumentTier3(Entity<MonumentComponent> uid)
    {
        uid.Comp.PercentageComplete = 0;
        CurrentTier = 3;
        uid.Comp.UnlockedInfluences.Add("InfluenceAstralLash");
        uid.Comp.UnlockedInfluences.Add("InfluenceNullGlare");
        _visibility.SetLayer(uid.Owner, 1, true);
        UpdateMonumentAppearance(uid, true);
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var _))
        {
            EnsureComp<CosmicStarMarkComponent>(cultist);
            EnsureComp<PressureImmunityComponent>(cultist);
            RemComp<TemperatureSpeedComponent>(cultist);
            RemComp<RespiratorComponent>(cultist);
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
    public void Finale(Entity<MonumentComponent> uid)
    {
        if (TryComp<CosmicCorruptingComponent>(uid, out var comp)) comp.Enabled = true;
        uid.Comp.FinaleReady = true;
        uid.Comp.EntropyUntilNextStage = 0;
        uid.Comp.CrewToConvertNextStage = 0;
        uid.Comp.PercentageComplete = 100;
        Log.Debug($"The monument is unleashed!"); //todo remove
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

        if (CurrentTier == 3)
        {
            EnsureComp<CosmicStarMarkComponent>(uid);
            EnsureComp<PressureImmunityComponent>(uid);
            EnsureComp<TemperatureImmunityComponent>(uid);
            RemComp<RespiratorComponent>(uid);
        }
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
        UpdateCultData(MonumentInGame);
    }
    private void OnShutdownCultist(Entity<CosmicCultComponent> uid, ref ComponentShutdown args)
    {
        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
        foreach (var actionEnt in uid.Comp.ActionEntities) _actions.RemoveAction(actionEnt);

        if (!TryComp<MindContainerComponent>(uid, out var mc))
            return;
        if (!_mind.TryGetMind(uid, out var mindId, out _, mc))
            return;
        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicDeconvertedEui(), session);
        }

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

        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return;
        _mind.ClearObjectives(mindId, mindComp); // LOAD-BEARING #imp function to remove all of someone's objectives, courtesy of TCRGDev(Github)
        _role.MindTryRemoveRole<CosmicCultRoleComponent>(mindId);
        _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
        TotalCult--;
        UpdateCultData(MonumentInGame);
    }
    #endregion
    private void DebugFunction(EntityUid uid, CosmicCultLeadComponent comp, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
    }

}
