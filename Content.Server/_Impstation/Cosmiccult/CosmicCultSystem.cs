using Content.Server.Popups;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Server.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Events;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Content.Shared._Impstation.Cosmiccult.Components.Examine;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _aud = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public EntProtoId CultToolPrototype = "AbilityCosmicCultTool";
    private const string MapPath = "Prototypes/_Impstation/CosmicCult/voidmap.yml";
    public int ObjectiveEntropyTracker = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentStartup>(OnStartCultLead);

        MakeSimpleExamineHandler<CosmicMarkStructureComponent>("cosmic-examine-text-structures");
        MakeSimpleExamineHandler<CosmicMarkBlankComponent>("cosmic-examine-text-abilityblank");

        SubscribeAbilities();
    }
    private void OnRoundStart(RoundStartingEvent ev)
    {
        _map.CreateMap(out var mapId);

        var options = new MapLoadOptions { LoadMap = true };
        if (_mapLoader.TryLoad(mapId, MapPath, out _, options))
            _map.SetPaused(mapId, false);
    }

    /// <summary>
    /// Called when the component initializes. We add the Visibility Mask here.
    /// </summary>
    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {

        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    /// <summary>
    /// Called when the component starts up, add the Cosmic Cult abilities to the user.
    /// </summary>
    private void OnStartCultist(EntityUid uid, CosmicCultComponent comp, ref ComponentStartup args)
    {
        foreach (var actionId in comp.BaseCosmicCultActions)
            _actions.AddAction(uid, actionId);
    }
    /// <summary>
    /// Called when the component starts up, add the Cosmic Cult monument ability to the user.
    /// </summary>
    private void OnStartCultLead(EntityUid uid, CosmicCultLeadComponent comp, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.MonumentActionEntity, comp.MonumentAction, uid);
    }

    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    private void IncrementCultObjectiveEntropy()
    {
        ObjectiveEntropyTracker++;
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
}
