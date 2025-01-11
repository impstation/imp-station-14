using Content.Server.Popups;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Server.Actions;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking.Events;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Content.Shared._Impstation.Cosmiccult.Components.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Roles;
using Content.Server.EUI;
using Content.Shared.Damage;
using Content.Server.Antag;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
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

    public EntProtoId CultToolPrototype = "AbilityCosmicCultTool";
    private const string MapPath = "Prototypes/_Impstation/CosmicCult/Maps/voidmap.yml";
    public readonly SoundSpecifier DeconvertSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_deconvert.ogg");
    public int ObjectiveEntropyTracker = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentStartup>(OnStartCultLead);

        SubscribeLocalEvent<CosmicCultComponent, ComponentShutdown>(OnShutdown);

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
                Log.Debug($"Sending {mindContainer.Mind} back to their body!");
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
    #endregion


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

    /// <summary>
    /// add cultist Visibility Mask.
    /// </summary>
    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {
        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    /// <summary>
    /// add the Cosmic Cult abilities to the cultist.
    /// </summary>
    private void OnStartCultist(EntityUid uid, CosmicCultComponent comp, ref ComponentStartup args)
    {
        EnsureComp<CosmicSpellSlotComponent>(uid, out var spell);
        _actions.AddAction(uid, ref spell.CosmicToolActionEntity, spell.CosmicToolAction, uid); // todo: award cult powers at The Monument
        _actions.AddAction(uid, ref spell.CosmicSiphonActionEntity, spell.CosmicSiphonAction, uid);
        _actions.AddAction(uid, ref spell.CosmicBlankActionEntity, spell.CosmicBlankAction, uid);
        _actions.AddAction(uid, ref spell.CosmicLapseActionEntity, spell.CosmicLapseAction, uid);
    }
    /// <summary>
    /// add the Cosmic Cult monument ability to the cult leader.
    /// </summary>
    private void OnStartCultLead(EntityUid uid, CosmicCultLeadComponent comp, ref ComponentStartup args)
    {
        EnsureComp<CosmicSpellSlotComponent>(uid, out var spell);
        _actions.AddAction(uid, ref spell.CosmicMonumentActionEntity, spell.CosmicMonumentAction, uid);
    }

    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    private void IncrementCultObjectiveEntropy()
    {
        ObjectiveEntropyTracker++;
    }


    /// <summary>
    /// Our horrible little function for
    /// </summary>
    private void OnShutdown(EntityUid uid, CosmicCultComponent comp, ref ComponentShutdown args)
    {
        if (!TryComp<CosmicSpellSlotComponent>(uid, out var spell))
            return;

        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
        _actions.RemoveAction(uid, spell.CosmicToolActionEntity); // todo: clean up cult powers better
        _actions.RemoveAction(uid, spell.CosmicSiphonActionEntity);
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

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), DeconvertSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);

        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return;
        _mind.ClearObjectives(mindId, mindComp); // LOAD-BEARING #imp function to remove all of someone's objectives, courtesy of TCRGDev(Github)
        _role.MindTryRemoveRole<CosmicCultRoleComponent>(mindId);
        _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
        _log.Add(LogType.Mind, LogImpact.Low, $"{uid} was Deconverted from the Cosmic Cult. Objectives removed from mind.");
    }

    private void DebugFunction(EntityUid uid, CosmicCultComponent comp, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
        if (_entMan.HasComponent<CosmicCultComponent>(uid))
        {
            _entMan.RemoveComponent<CosmicCultComponent>(uid);
            _entMan.RemoveComponent<ActiveRadioComponent>(uid);
            _entMan.RemoveComponent<CleanseCorruptionComponent>(uid);
            _entMan.RemoveComponent<IntrinsicRadioReceiverComponent>(uid);
            _entMan.RemoveComponent<IntrinsicRadioTransmitterComponent>(uid);

            if (_entMan.HasComponent<CosmicCultLeadComponent>(uid))
                _entMan.RemoveComponent<CosmicCultLeadComponent>(uid);
        }
    }

}
