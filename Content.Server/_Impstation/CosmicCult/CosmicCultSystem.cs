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

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string MapPath = "Prototypes/_Impstation/CosmicCult/Maps/voidmap.yml";
    public readonly SoundSpecifier DeconvertSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_deconvert.ogg");
    public int ObjectiveEntropyTracker = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultComponent, ComponentShutdown>(OnShutdownCultist);
        SubscribeLocalEvent<MonumentComponent, ComponentInit>(OnStartMonument);
        SubscribeLocalEvent<MonumentComponent, InteractUsingEvent>(OnInteractUsing);

        MakeSimpleExamineHandler<CosmicMarkStructureComponent>("cosmic-examine-text-structures");
        MakeSimpleExamineHandler<CosmicMarkBlankComponent>("cosmic-examine-text-abilityblank");
        MakeSimpleExamineHandler<CosmicMarkLapseComponent>("cosmic-examine-text-abilitylapse");

        SubscribeAbilities();
    }
    #region Housekeeping
    private void OnRoundStart(RoundStartingEvent ev)
    {
        _map.CreateMap(out var mapId);

        var options = new MapLoadOptions { LoadMap = true };
        if (_mapLoader.TryLoad(mapId, MapPath, out _, options))
            _map.SetPaused(mapId, false);
    }
    /// <summary>
    /// Creates the Cosmic Void pocket dimension map.
    /// </summary>
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
    /// <summary>
    /// add the Cosmic Cult abilities to the cultist.
    /// </summary>
    private void OnStartCultist(Entity<CosmicCultComponent> uid, ref ComponentInit args)
    {
        EnsureComp<CosmicSpellSlotComponent>(uid, out var spell);
        _actions.AddAction(uid, ref spell.CosmicSiphonActionEntity, spell.CosmicSiphonAction, uid); // TODO: award cult powers differently
        _actions.AddAction(uid, ref spell.CosmicBlankActionEntity, spell.CosmicBlankAction, uid);
        if (HasComp<CosmicCultLeadComponent>(uid))
            _actions.AddAction(uid, ref spell.CosmicMonumentActionEntity, spell.CosmicMonumentAction, uid);
        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | MonumentComponent.LayerMask);
    }

    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    private void IncrementCultObjectiveEntropy(Entity<CosmicCultComponent> uid)
    {
        ObjectiveEntropyTracker += uid.Comp.CosmicSiphonQuantity;
        var query = EntityQueryEnumerator<CosmicEntropyConditionComponent>();
        while (query.MoveNext(out var _, out var entropyComp))
        {
            entropyComp.Siphoned = ObjectiveEntropyTracker;
        }
    }

    private void OnStartMonument(Entity<MonumentComponent> uid, ref ComponentInit args)
    {
        var monument = uid.Comp;
        _cultRule.CultTier1(uid);
        _cultRule.UpdateCultData(uid);
        monument.CrewToConvertNextStage = _cultRule.CrewTillNextTier;
    }

    private void UpdateEntropyMetrics(Entity<MonumentComponent> uid)
    {
    }

    private void OnInteractUsing(Entity<MonumentComponent> uid, ref InteractUsingEvent args)
    {
        if (!HasComp<CosmicEntropyMoteComponent>(args.Used) || args.Handled)
            return;
        args.Handled = AddEntropy(uid, args.Used, args.User);
    }

    private bool AddEntropy(Entity<MonumentComponent> monument, EntityUid entropy, EntityUid cultist)
    {
        var quant = TryComp<StackComponent>(entropy, out var stackComp) ? stackComp.Count : 1;
        Log.Debug($"Adding {quant} entropy!");
        monument.Comp.InfusedEntropy += quant;
        monument.Comp.AvailableEntropy += quant;
        QueueDel(entropy);
        UpdateEntropyMetrics(monument);
        return true;
    }


    /// <summary>
    /// Our horrible little function for when a cultist gets deconverted. This is surely awful, but very straightforward.
    /// </summary>
    private void OnShutdownCultist(Entity<CosmicCultComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<CosmicSpellSlotComponent>(uid, out var spell))
            return;

        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
        _actions.RemoveAction(uid, spell.CosmicSiphonActionEntity); // TODO: clean up cult powers better
        _actions.RemoveAction(uid, spell.CosmicBlankActionEntity);
        _actions.RemoveAction(uid, spell.CosmicLapseActionEntity);
        _actions.RemoveAction(uid, spell.CosmicMonumentActionEntity);

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

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), DeconvertSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);

        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return;
        _mind.ClearObjectives(mindId, mindComp); // LOAD-BEARING #imp function to remove all of someone's objectives, courtesy of TCRGDev(Github)
        _role.MindTryRemoveRole<CosmicCultRoleComponent>(mindId);
        _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
        _cultRule.TotalCult--;
        _log.Add(LogType.Mind, LogImpact.Low, $"{uid.Owner} was Deconverted from the Cosmic Cult. All objectives removed from mind.");
    }

    private void DebugFunction(Entity<CosmicCultComponent> uid, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
        // _cleanse.DeconvertCultist(uid);
    }

}
