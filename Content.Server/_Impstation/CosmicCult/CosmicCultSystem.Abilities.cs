using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared.Actions;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Maps;
using Content.Server.Objectives.Components;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.Network;
using Content.Server.Station.Systems;
using Content.Server.Station.Components;
using Content.Server.Bible.Components;
using Content.Shared.Humanoid;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using Content.Server.Antag.Components;
using System.Collections.Immutable;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components.Examine;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly CosmicCultRuleSystem _cosmicCultRule = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entLookup = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly StationSpawningSystem _spawningSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicToolToggle>(OnCosmicToolToggle);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlankDoAfter>(OnCosmicBlankDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlank>(OnCosmicBlank);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicLapse>(OnCosmicLapse);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
    }

    /// <summary>
    /// A 'lil catch-all thing to double check.. stuff. Called by multiple abilities.
    /// </summary>
    public bool TryUseAbility(EntityUid uid, CosmicCultComponent comp, BaseActionEvent action)
    {
        if (action.Handled)
            return false;
        if (!TryComp<CosmicCultActionComponent>(action.Action, out var cultAction))
            return false;
        action.Handled = true;
        return true;
    }

    #region Siphon Entropy
    /// <summary>
    /// Called when someone clicks on a target using the cosmic siphon ability.
    /// </summary>
    private void OnCosmicSiphon(EntityUid uid, CosmicCultComponent comp, ref EventCosmicSiphon action)
    {
        if (HasComp<CosmicCultComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target)) // the BaseAction system doesn't have a blacklist. This acts as one. Blacklist cultists and the chaplain.
            return;
        if (!TryUseAbility(uid, comp, action))
            return;

        var doargs = new DoAfterArgs(EntityManager, uid, comp.CosmicSiphonSpeed, new EventCosmicSiphonDoAfter(), uid, action.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    /// <summary>
    /// Called when CosmicSiphon's DoAfter completes.
    /// </summary>
    private void OnCosmicSiphonDoAfter(EntityUid uid, CosmicCultComponent comp, EventCosmicSiphonDoAfter action)
    {
        if (action.Args.Target == null)
            return;

        var target = action.Args.Target.Value;
        if (action.Cancelled || action.Handled)
            return;

        action.Handled = true;
        _damageable.TryChangeDamage(action.Target, comp.CosmicSiphonDamage, origin: uid);
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);

        var entropymote1 = SpawnAtPosition(comp.CosmicSiphonResult, Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(uid, entropymote1);

        // increment the greentext tracker and then set our tracker to what the greentext's tracker currently is
        IncrementCultObjectiveEntropy();
        if (_mind.TryGetObjectiveComp<CosmicEntropyConditionComponent>(uid, out var obj))
            obj.Siphoned = ObjectiveEntropyTracker;
    }
    #endregion


    #region Compass Toggle
    /// <summary>
    /// Called when someone uses the Beckon Compass ability.
    /// </summary>
    private void OnCosmicToolToggle(EntityUid uid, CosmicCultComponent comp, ref EventCosmicToolToggle action)
    {

        if (!TryUseAbility(uid, comp, action))
            return;

        if (!TryToggleCosmicTool(uid, CultToolPrototype, comp))
            return;
    }

    /// <summary>
    /// Called by the Beckon Compass ability's OnCosmicToolToggle. Why are we nesting it like this? Fucked if i know.
    /// </summary>
    public bool TryToggleCosmicTool(EntityUid uid, EntProtoId proto, CosmicCultComponent comp)
    {
        if (!comp.Equipment.TryGetValue(proto.Id, out var item))
        {
            item = Spawn(proto, Transform(uid).Coordinates);
            if (!_hands.TryForcePickupAnyHand(uid, (EntityUid)item))
            {
                _popup.PopupEntity(Loc.GetString("cosmicability-toggle-error"), uid, uid);
                QueueDel(item);
                return false;
            }
            comp.Equipment.Add(proto.Id, item);
            return true;
        }

        QueueDel(item);
        // assuming that it exists
        comp.Equipment.Remove(proto.Id);

        return true;
    }
    #endregion


    #region "Blank" Stun
    /// <summary>
    /// Called when someone clicks on a target using the cosmic blank ability.
    /// </summary>
    private void OnCosmicBlank(EntityUid uid, CosmicCultComponent comp, ref EventCosmicBlank action)
    {
        if (HasComp<CosmicCultComponent>(action.Target) || HasComp<CosmicMarkBlankComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target)) // Blacklist the chaplain, obviously.
            return;
        if (!TryUseAbility(uid, comp, action))
            return;

        var tgtpos = Transform(action.Target).Coordinates;

        var doargs = new DoAfterArgs(EntityManager, uid, comp.CosmicBlankSpeed, new EventCosmicBlankDoAfter(), uid, action.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        _doAfter.TryStartDoAfter(doargs);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-begin", ("target", Identity.Entity(uid, EntityManager))), uid, action.Target);
    }

    /// <summary>
    /// Called when CosmicBlank's DoAfter completes.
    /// </summary>
    private void OnCosmicBlankDoAfter(EntityUid uid, CosmicCultComponent comp, EventCosmicBlankDoAfter action)
    {
        if (action.Args.Target == null)
            return;
        var target = action.Args.Target.Value;
        if (action.Cancelled || action.Handled)
            return;
        action.Handled = true;
        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || !mindContainer.HasMind)
        {
            Log.Debug($"Couldn't find a mindcontainer for {target}.");
            return;
        }

        Log.Debug($"Sending {mindContainer.Mind} to the cosmic void!");
        EnsureComp<CosmicMarkBlankComponent>(target);

        _popup.PopupEntity(Loc.GetString("cosmicability-blank-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        var tgtpos = Transform(target).Coordinates;
        var mindEnt = mindContainer.Mind.Value;
        var mind = Comp<MindComponent>(mindEnt);
        mind.PreventGhosting = true;

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        if (spawnPoints.IsEmpty)
        {
            Log.Warning("Couldn't find any cosmic void spawners! Failed to send.");
            return;
        }
        _audio.PlayPvs(comp.BlankSFX, uid, AudioParams.Default.WithVolume(+6f));
        Spawn(comp.BlankVFX, tgtpos);
        var newSpawn = _random.Pick(spawnPoints);
        var spawnTgt = Transform(newSpawn.Uid).Coordinates;
        var mobUid = Spawn(comp.SpawnWisp, spawnTgt);
        EnsureComp<AntagImmuneComponent>(mobUid);
        EnsureComp<InVoidComponent>(mobUid, out var inVoid);
        inVoid.OriginalBody = target;
        inVoid.ExitVoidTime = _timing.CurTime + comp.CosmicBlankDuration;
        _mind.TransferTo(mindEnt, mobUid);
        _stun.TryKnockdown(target, comp.CosmicBlankDuration, true);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-transfer"), mobUid, mobUid);
        _audio.PlayPvs(comp.BlankSFX, spawnTgt, AudioParams.Default.WithVolume(+6f));
        Spawn(comp.BlankVFX, spawnTgt);

        Log.Debug($"Created wisp entity {mobUid}");
    }
    #endregion

    #region "Lapse" Polymorph
    /// <summary>
    /// Called when someone clicks on a target using the cosmic lapse ability.
    /// </summary>
    private void OnCosmicLapse(EntityUid uid, CosmicCultComponent comp, ref EventCosmicLapse action)
    {
        if (action.Handled || HasComp<CosmicMarkBlankComponent>(action.Target) || HasComp<CleanseCorruptionComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target)) // Blacklist the chaplain, obviously.
            return;
        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        _audio.PlayPvs(comp.LapseSFX, uid, AudioParams.Default.WithVolume(+6f));
        Spawn(comp.LapseVFX, tgtpos);
        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success", ("target", Identity.Entity(action.Target, EntityManager))), uid, uid);
        TryComp<HumanoidAppearanceComponent>(action.Target, out HumanoidAppearanceComponent? species);
        switch (species!.Species) // We use a switch case for all the species variants. Why? It uses tidy wizden code, leans on YML, and it's pretty efficient.
        {
            case "Human":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobHuman");
                break;
            case "Arachnid":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobArachnid");
                break;
            case "Diona":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobDiona");
                break;
            case "Moth":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobMoth");
                break;
            case "Vox":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobVox");
                break;
            case "Snail":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobSnail");
                break;
            case "Decapoid":
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobDecapoid");
                break;
            default:
                _polymorphSystem.PolymorphEntity(action.Target, "CosmicLapseMobHuman");
                break;
        }
    }
    #endregion

    #region MonumentSpawn
    /// <summary>
    /// Called when the cult lead attemps to place The Monument. This code is awful, but at least it's straightforward.
    /// </summary>
    private void OnCosmicPlaceMonument(EntityUid uid, CosmicCultLeadComponent comp, ref EventCosmicPlaceMonument args)
    {
        if (!TryComp<CosmicSpellSlotComponent>(uid, out var spell))
            return;

        var spaceDistance = 3;
        var xform = Transform(uid);
        var user = Transform(args.Performer);
        var worldPos = _transform.GetWorldPosition(xform);
        var pos = xform.LocalPosition;
        var box = new Box2(pos + new Vector2(-1.4f, -0.4f), pos + new Vector2(1.4f, 0.4f));

        /// MAKE SURE WE'RE STANDING ON A GRID, BRUH.
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
        {
            _popup.PopupEntity(Loc.GetString("cosmic-monument-spawn-error-grid"), uid, uid);
            return;
        }

        /// CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (!tile.IsSpace(_tileDef))
                continue;

            _popup.PopupEntity(Loc.GetString("cosmic-monument-spawn-error-space", ("DISTANCE", spaceDistance)), uid, uid);
            return;
        }

        /// CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        var station = _station.GetStationInMap(xform.MapID);
        EntityUid? stationGrid = null;
        if (TryComp<StationDataComponent>(station, out var stationData))
            stationGrid = _station.GetLargestGrid(stationData);
        if (stationGrid is not null && stationGrid != xform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("cosmic-monument-spawn-error-station"), uid, uid);
            return;
        }

        ///CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS || I HATED THIS SO FREAKING MUCH.
        if (_entLookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, uid))
        {
            _popup.PopupEntity(Loc.GetString("cosmic-monument-spawn-error-intersection"), uid, uid);
            return;
        }

        _actions.RemoveAction(uid, spell.CosmicMonumentActionEntity);
        Spawn(comp.MonumentPrototype, _transform.GetMapCoordinates(uid, xform: xform));
    }
    #endregion
}
