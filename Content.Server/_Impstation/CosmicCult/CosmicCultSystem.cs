using Content.Server.Popups;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Server.Actions;
using Content.Server.GameTicking.Events;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Roles;
using Content.Server.EUI;
using Content.Shared.Damage;
using Content.Server.Antag;
using Robust.Shared.Audio;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Timing;
using Content.Server.Stack;
using Content.Server.Objectives.Components;
using Content.Server.Radio.Components;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Robust.Server.Player;
using Content.Server.Announcements.Systems;
using Content.Server.Audio;
using Content.Shared.Audio;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly CosmicGlyphSystem _cosmicGlyphs = default!;
    private const string MapPath = "Prototypes/_Impstation/CosmicCult/Maps/cosmicvoid.yml";
    public int _cultistCount;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentInit>(OnStartCultLead);
        SubscribeLocalEvent<MonumentComponent, ComponentInit>(OnStartMonument);
        SubscribeLocalEvent<MonumentComponent, InteractUsingEvent>(OnInteractUsing);

        MakeSimpleExamineHandler<CosmicMarkStructureComponent>("cosmic-examine-text-structures");
        MakeSimpleExamineHandler<CosmicMarkBlankComponent>("cosmic-examine-text-abilityblank");
        MakeSimpleExamineHandler<CosmicMarkLapseComponent>("cosmic-examine-text-abilitylapse");

        SubscribeAbilities(); //Hook up the cosmic cult ability system
        SubscribeFinale(); //Hook up the cosmic cult finale system
    }
    #region Housekeeping

    /// <summary>
    /// Creates the Cosmic Void pocket dimension map.
    /// </summary>
    private void OnRoundStart(RoundStartingEvent ev)
    {
        _map.CreateMap(out var mapId);
        var options = new MapLoadOptions { LoadMap = true };
        if (_mapLoader.TryLoad(mapId, MapPath, out _, options))
            _map.SetPaused(mapId, false);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blanktimer = EntityQueryEnumerator<InVoidComponent>();
        while (blanktimer.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExitVoidTime)
            {
                if (!TryComp<MindContainerComponent>(uid, out var mindContainer))
                    continue;
                var mindEnt = mindContainer.Mind!.Value;
                var mind = Comp<MindComponent>(mindEnt);
                mind.PreventGhosting = false;
                _mind.TransferTo(mindEnt, comp.OriginalBody);
                RemComp<CosmicMarkBlankComponent>(comp.OriginalBody);
                _popup.PopupEntity(Loc.GetString("cosmicability-blank-return"), comp.OriginalBody, comp.OriginalBody);
                QueueDel(uid);
            }
        }
        var finaleQuery = EntityQueryEnumerator<CosmicFinaleComponent>();
        while (finaleQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.FinaleActive && !comp.BufferComplete && !comp.PlayedBufferSong && !string.IsNullOrEmpty(_selectedBufferSong))
            {
                _sound.DispatchStationEventMusic(uid, _selectedBufferSong, StationEventMusicType.Nuke);
                comp.PlayedBufferSong = true;
                Log.Debug($"Buffer now running.");
            }
            else if (comp.FinaleActive && comp.FinaleTimer <= _finaleSongLength + comp.SummoningTime && !comp.PlayedFinaleSong && !string.IsNullOrEmpty(_selectedFinaleSong) && comp.BufferComplete && !comp.PlayedFinaleSong)
            {
                _sound.DispatchStationEventMusic(uid, _selectedFinaleSong, StationEventMusicType.Nuke);
                comp.PlayedFinaleSong = true;
                Log.Debug($"Finale now running.");
            }
            if (comp.FinaleActive && _timing.CurTime >= comp.BufferTimer && comp.FinaleActive && !comp.BufferComplete && !comp.Victory)
            {
                _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
                Log.Debug($"Buffer complete.");
                comp.FinaleTimer = _timing.CurTime + comp.FinaleRemainingTime;
                _selectedFinaleSong = _audio.GetSound(_finaleMusic);
                _finaleSongLength = TimeSpan.FromSeconds(_audio.GetAudioLength(_selectedFinaleSong).TotalSeconds);
                _sound.DispatchStationEventMusic(uid, _selectedFinaleSong, StationEventMusicType.Nuke);
                comp.BufferComplete = true;
                comp.PlayedFinaleSong = true;
                Log.Debug($"Finale now running.");
            }
            else if (comp.FinaleActive && _timing.CurTime >= comp.FinaleTimer && comp.FinaleActive && comp.BufferComplete && !comp.Victory)
            {
                Log.Debug($"Finale complete.");
                _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
                Spawn("MobCosmicGodSpawn", Transform(uid).Coordinates);
                comp.Victory = true;
            }
            if (_timing.CurTime >= comp.CultistsCheckTimer && comp.FinaleActive && !comp.BufferComplete)
            {
                comp.CultistsCheckTimer = _timing.CurTime + comp.CheckWait;
                var cultistsPresent = _cultistCount = _cosmicGlyphs.GatherCultists(uid, 5).Count; //Let's use the cultist collecting hashset from Cosmic Glyphs to see how many folks are around!
                if (cultistsPresent <= 10) _cultistCount = cultistsPresent;
                Log.Debug($"{_cultistCount} cultists near The Monument.");
                Log.Debug($"reducing buffertimer by {_timing.TickPeriod * (4 * _cultistCount * 0.1)} per tick.");
            }
            if (comp.FinaleActive && !comp.BufferComplete)
            {
                comp.BufferTimer -= _timing.TickPeriod * (4 * _cultistCount * 0.1); //This dynamically reduces the duration of the buffer by # cultists present at The Monument.
            }
        }
    }

    /// <summary>
    /// Parses marker components to output their respective loc strings directly into your examine box, courtesy of TGRCdev(Github).
    /// </summary>
    private void MakeSimpleExamineHandler<TComp>(LocId message)
    where TComp: IComponent
    {
        SubscribeLocalEvent((Entity<TComp> ent, ref ExaminedEvent args) => {
            if (HasComp<CosmicCultComponent>(args.Examiner))
                args.PushMarkup(Loc.GetString("cosmic-examine-text-forthecult"));
            else
                args.PushMarkup(Loc.GetString(message, ("entity", ent.Owner)));
        });
    }
    #endregion
    #region Init Cult
    /// <summary>
    /// Add the starting powers to the cultist.
    /// </summary>
    private void OnStartCultist(Entity<CosmicCultComponent> uid, ref ComponentInit args)
    {
        foreach (var actionId in uid.Comp.CosmicCultActions)
        {
            var actionEnt = _actions.AddAction(uid, actionId);
            uid.Comp.ActionEntities.Add(actionEnt);
        }
        if (TryComp<CosmicCultLeadComponent>(uid, out var leadComp)) _actions.AddAction(uid, leadComp.CosmicMonumentAction);
        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | MonumentComponent.LayerMask);
    }
    /// <summary>
    /// Add the Monument summon action to the cult lead.
    /// </summary>
    private void OnStartCultLead(Entity<CosmicCultLeadComponent> uid, ref ComponentInit args)
    {
        _actions.AddAction(uid, ref uid.Comp.CosmicMonumentActionEntity, uid.Comp.CosmicMonumentAction, uid);
    }
    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    #endregion
    #region Entropy
    private void OnStartMonument(Entity<MonumentComponent> uid, ref ComponentInit args)
    {
        _cultRule.MonumentTier1(uid);
        _cultRule.UpdateCultData(uid);
    }

    private void OnInteractUsing(Entity<MonumentComponent> uid, ref InteractUsingEvent args)
    {
        if (!HasComp<CosmicEntropyMoteComponent>(args.Used) || !HasComp<CosmicCultComponent>(args.User) || !uid.Comp.Enabled || args.Handled)
            return;
        args.Handled = AddEntropy(uid, args.Used, args.User);
    }
    private bool AddEntropy(Entity<MonumentComponent> monument, EntityUid entropy, EntityUid cultist)
    {
        var quant = TryComp<StackComponent>(entropy, out var stackComp) ? stackComp.Count : 1;
        if (TryComp<CosmicCultComponent>(cultist, out var cultComp))
            cultComp.EntropyBudget += quant;
        monument.Comp.TotalEntropy += quant;
        _cultRule.TotalEntropy += quant;
        _cultRule.UpdateCultData(monument);
        _popup.PopupEntity(Loc.GetString("cosmiccult-entropy-inserted", ("count", quant)), cultist, cultist);
        _audio.PlayEntity("/Audio/_Impstation/CosmicCult/cosmiclance_hit.ogg", cultist, monument); //TODO: INSERTION SOUND EFFECT
        QueueDel(entropy);
        return true;
    }
    #endregion
}
