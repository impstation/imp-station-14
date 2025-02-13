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
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;    [Dependency] private readonly IGameTiming _timing = default!;

    private const string MapPath = "Prototypes/_Impstation/CosmicCult/Maps/cosmicvoid.yml";
    public int ObjectiveEntropyTracker = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnStartCultist);
        SubscribeLocalEvent<MonumentComponent, ComponentInit>(OnStartMonument);
        SubscribeLocalEvent<MonumentComponent, InteractUsingEvent>(OnInteractUsing);

        MakeSimpleExamineHandler<CosmicMarkStructureComponent>("cosmic-examine-text-structures");
        MakeSimpleExamineHandler<CosmicMarkBlankComponent>("cosmic-examine-text-abilityblank");
        MakeSimpleExamineHandler<CosmicMarkLapseComponent>("cosmic-examine-text-abilitylapse");

        SubscribeAbilities();
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
        _cultRule.MonumentTier1(uid);
        _cultRule.UpdateCultData(uid);
    }

    private void OnInteractUsing(Entity<MonumentComponent> uid, ref InteractUsingEvent args)
    {
        if (!HasComp<CosmicEntropyMoteComponent>(args.Used) || uid.Comp.FinaleReady || args.Handled)
            return;
        args.Handled = AddEntropy(uid, args.Used, args.User);
    }

    private bool AddEntropy(Entity<MonumentComponent> monument, EntityUid entropy, EntityUid cultist)
    {
        var quant = TryComp<StackComponent>(entropy, out var stackComp) ? stackComp.Count : 1;
        Log.Debug($"Adding {quant} entropy!");
        monument.Comp.TotalEntropy += quant;
        monument.Comp.AvailableEntropy += quant;
        QueueDel(entropy);
        _cultRule.UpdateCultData(monument);
        return true;
    }


    private void DebugFunction(Entity<CosmicCultComponent> uid, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
        // _cleanse.DeconvertCultist(uid);
    }

}
