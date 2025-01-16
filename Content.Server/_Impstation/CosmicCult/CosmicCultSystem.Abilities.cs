using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.Actions;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Maps;
using Content.Server.Objectives.Components;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using System.Numerics;
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
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entLookup = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlankDoAfter>(OnCosmicBlankDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlank>(OnCosmicBlank);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicLapse>(OnCosmicLapse);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
    }


    #region Siphon Entropy
    private void OnCosmicSiphon(Entity<CosmicCultComponent> uid, ref EventCosmicSiphon args)
    {
        if (HasComp<CosmicCultComponent>(args.Target) || HasComp<BibleUserComponent>(args.Target))
            return;
        if (args.Handled)
            return;
        args.Handled = true;

        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.CosmicSiphonSpeed, new EventCosmicSiphonDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        _doAfter.TryStartDoAfter(doargs);
    }



    private void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        _damageable.TryChangeDamage(args.Target, uid.Comp.CosmicSiphonDamage, origin: uid);
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);

        var entropymote1 = SpawnAtPosition(uid.Comp.CosmicSiphonResult, Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(uid, entropymote1);

        // increment the greentext tracker and then set our tracker to what the greentext's tracker currently is
        IncrementCultObjectiveEntropy();
        if (_mind.TryGetObjectiveComp<CosmicEntropyConditionComponent>(uid, out var obj))
            obj.Siphoned = ObjectiveEntropyTracker;
    }
    #endregion



    #region "Blank" Stun
    private void OnCosmicBlank(Entity<CosmicCultComponent> uid, ref EventCosmicBlank args)
    {
        if (HasComp<CosmicCultComponent>(args.Target) || HasComp<CosmicMarkBlankComponent>(args.Target) || HasComp<BibleUserComponent>(args.Target)) // Blacklist the chaplain, obviously.
            return;
        if (args.Handled)
            return;

        var tgtpos = Transform(args.Target).Coordinates;

        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.CosmicBlankSpeed, new EventCosmicBlankDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        _doAfter.TryStartDoAfter(doargs);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-begin", ("target", Identity.Entity(uid, EntityManager))), uid, args.Target);
    }

    private void OnCosmicBlankDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicBlankDoAfter args)
    {
        if (args.Args.Target == null)
            return;
        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;

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
        var comp = uid.Comp;
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
    private void OnCosmicLapse(Entity<CosmicCultComponent> uid, ref EventCosmicLapse action)
    {
        if (action.Handled || HasComp<CosmicMarkBlankComponent>(action.Target) || HasComp<CleanseCultComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target)) // Blacklist the chaplain, obviously.
            return;
        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        Spawn(uid.Comp.LapseVFX, tgtpos);
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
    private void OnCosmicPlaceMonument(Entity<CosmicCultLeadComponent> uid, ref EventCosmicPlaceMonument args)
    {
        if (!TryComp<CosmicSpellSlotComponent>(uid, out var spell))
            return;

        var spaceDistance = 3;
        var xform = Transform(uid);
        var user = Transform(args.Performer);
        var worldPos = _transform.GetWorldPosition(xform);
        var pos = xform.LocalPosition;
        var box = new Box2(pos + new Vector2(-1.4f, -0.4f), pos + new Vector2(1.4f, 0.4f));

        /// MAKE SURE WE'RE STANDING ON A GRID
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
        ///CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_entLookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, uid))
        {
            _popup.PopupEntity(Loc.GetString("cosmic-monument-spawn-error-intersection"), uid, uid);
            return;
        }

        _actions.RemoveAction(uid, spell.CosmicMonumentActionEntity);
        Spawn(uid.Comp.MonumentPrototype, _transform.GetMapCoordinates(uid, xform: xform));
    }
    #endregion
}
