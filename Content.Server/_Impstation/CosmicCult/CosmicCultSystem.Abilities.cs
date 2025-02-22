using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Maps;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using System.Numerics;
using Content.Server.Station.Systems;
using Content.Server.Station.Components;
using Content.Server.Bible.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using Content.Server.Antag.Components;
using System.Collections.Immutable;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Content.Shared.Prying.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Lock;
using Content.Server.Doors.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Flash;
using Content.Shared.Camera;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Content.Server.Flash;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Events;
using Content.Shared.Tag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Shared.Atmos;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicImposingComponent, BeforeDamageChangedEvent>(OnImpositionDamaged);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicIngress>(OnCosmicIngress);
        SubscribeLocalEvent<CosmicCultComponent, EventForceIngressDoAfter>(OnCosmicIngressDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicGlare>(OnCosmicGlare);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicNova>(OnCosmicNova);
        SubscribeLocalEvent<CosmicAstralNovaComponent, StartCollideEvent>(OnNovaCollide);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicImposition>(OnCosmicImposition);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlankDoAfter>(OnCosmicBlankDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlank>(OnCosmicBlank);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicLapse>(OnCosmicLapse);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
        SubscribeLocalEvent<CosmicAstralBodyComponent, EventCosmicReturn>(OnCosmicReturn);
    }

    #region Force Ingress
    private void OnCosmicIngress(Entity<CosmicCultComponent> uid, ref EventCosmicIngress args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        if (!uid.Comp.CosmicEmpowered)
        {
            var doargs = new DoAfterArgs(EntityManager, uid, 6, new EventForceIngressDoAfter(), uid, args.Target)
            {
                DistanceThreshold = 1.5f,
                Hidden = true,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnDropItem = true,
            };
            _doAfter.TryStartDoAfter(doargs);
        }
        else
        {
            var doargs = new DoAfterArgs(EntityManager, uid, 4, new EventForceIngressDoAfter(), uid, args.Target)
            {
                DistanceThreshold = 1.5f,
                Hidden = true,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnDropItem = true,
            };
            _doAfter.TryStartDoAfter(doargs);
        }

    }
    private void OnCosmicIngressDoAfter(Entity<CosmicCultComponent> uid, ref EventForceIngressDoAfter args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        _door.StartOpening(target);
        _audio.PlayPvs(uid.Comp.IngressSFX, uid);
        Spawn(uid.Comp.AbsorbVFX, Transform(target).Coordinates);
    }
    #endregion



    #region Null Glare
    private void OnCosmicGlare(Entity<CosmicCultComponent> uid, ref EventCosmicGlare args)
    {
        _audio.PlayPvs(uid.Comp.GlareSFX, uid);
        Spawn(uid.Comp.GlareVFX, Transform(uid).Coordinates);
        args.Handled = true;
        var mapPos = _transform.GetMapCoordinates(args.Performer);
        var targets = Filter.Empty();
        targets.AddInRange(mapPos, 10, _playerMan, EntityManager);
        foreach (var target in targets.Recipients)
        {
            if (target.AttachedEntity is { } entity)
            {
                if (HasComp<CosmicCultComponent>(entity) || HasComp<BibleUserComponent>(entity))
                    return;
                var hitPos = _transform.GetMapCoordinates(entity).Position;
                var angle = hitPos - mapPos.Position;
                if (angle == Vector2.Zero)
                    continue;
                if (angle.EqualsApprox(Vector2.Zero))
                    angle = new(.01f, 0);

                _recoil.KickCamera(entity, -angle.Normalized());
                if (!uid.Comp.CosmicEmpowered)
                    _flash.Flash(entity, uid, args.Action, 6 * 1000f, 0.5f);
                else _flash.Flash(entity, uid, args.Action, 9 * 1000f, 0.5f);
            }
        }

        var entities = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10);
        entities.RemoveWhere(entity => !HasComp<PoweredLightComponent>(entity));
        foreach (var entity in entities)
            _poweredLight.TryDestroyBulb(entity);
    }
    #endregion



    #region Astral Nova
    private void OnCosmicNova(Entity<CosmicCultComponent> uid, ref EventCosmicNova args) // This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    {
        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);

        var angle = targetPos.Position - startPos.Position;
        if (angle.EqualsApprox(Vector2.Zero))
            angle = new(.01f, 0);

        args.Handled = true;
        var ent = Spawn("ProjectileCosmicNova", startPos);
        _gunSystem.ShootProjectile(ent, angle, userVelocity, args.Performer, args.Performer, 5f);
        _audio.PlayPvs(uid.Comp.NovaCastSFX, uid, AudioParams.Default.WithVariation(0.1f));
    }

    private void OnNovaCollide(Entity<CosmicAstralNovaComponent> uid, ref StartCollideEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.OtherEntity) || HasComp<BibleUserComponent>(args.OtherEntity))
            return;

        _stun.TryParalyze(args.OtherEntity, TimeSpan.FromSeconds(2f), false);
        _damageable.TryChangeDamage(args.OtherEntity, uid.Comp.CosmicNovaDamage); // This'll probably trigger two or three times because of how collision works. I'm not being lazy here, it's a feature (kinda /s)
    }
    #endregion



    #region Vacuous Imposition
    private void OnCosmicImposition(Entity<CosmicCultComponent> uid, ref EventCosmicImposition args)
    {
        EnsureComp<CosmicImposingComponent>(uid, out var comp);
        if (!uid.Comp.CosmicEmpowered) comp.ImposeCheckTimer = _timing.CurTime + comp.CheckWait;
        else comp.ImposeCheckTimer = _timing.CurTime + comp.EmpoweredCheckWait;
        Spawn(uid.Comp.ImpositionVFX, Transform(uid).Coordinates);
        args.Handled = true;
        _audio.PlayPvs(uid.Comp.ImpositionSFX, uid, AudioParams.Default.WithVariation(0.05f));
    }
    private void OnImpositionDamaged(Entity<CosmicImposingComponent> uid, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
    #endregion


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
            BreakOnHandChange = true,
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

        var entropymote1 = _stack.Spawn(uid.Comp.CosmicSiphonQuantity, "Entropy", Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(uid, entropymote1);
        _cultRule.IncrementCultObjectiveEntropy(uid);
    }
    #endregion



    #region "Shunt" Stun
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
        if (action.Handled || HasComp<CosmicMarkBlankComponent>(action.Target) || HasComp<CleanseCultComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target))
            return;
        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        Spawn(uid.Comp.LapseVFX, tgtpos);
        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success", ("target", Identity.Entity(action.Target, EntityManager))), uid, uid);
        TryComp<HumanoidAppearanceComponent>(action.Target, out HumanoidAppearanceComponent? species);
        switch (species!.Species) // We use a switch case for all the species polymorphs. Why? It uses wizden code, leans on YML, and it could be worse.
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
        var spaceDistance = 3;
        var xform = Transform(uid);
        var user = Transform(args.Performer);
        var worldPos = _transform.GetWorldPosition(xform);
        var pos = xform.LocalPosition + new Vector2(0, 1f);
        var box = new Box2(pos + new Vector2(-1.4f, -0.4f), pos + new Vector2(1.4f, 0.4f));

        /// MAKE SURE WE'RE STANDING ON A GRID
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-grid"), uid, uid);
            return;
        }
        /// CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (!tile.IsSpace(_tileDef))
                continue;

            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-space", ("DISTANCE", spaceDistance)), uid, uid);
            return;
        }
        /// CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        var station = _station.GetStationInMap(xform.MapID);
        EntityUid? stationGrid = null;
        if (TryComp<StationDataComponent>(station, out var stationData))
            stationGrid = _station.GetLargestGrid(stationData);
        if (stationGrid is not null && stationGrid != xform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-station"), uid, uid);
            return;
        }
        ///CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, uid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-intersection"), uid, uid);
            return;
        }
        _actions.RemoveAction(uid, uid.Comp.CosmicMonumentActionEntity);
        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        Spawn(uid.Comp.MonumentPrototype, _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid));
    }
    #endregion

    #region Return (for Glyph)
    private void OnCosmicReturn(Entity<CosmicAstralBodyComponent> uid, ref EventCosmicReturn args) //This action is exclusive to the Glyph-created Astral Projection, and allows the user to return to their original body.
    {
        if (_mind.TryGetMind(args.Performer, out var mindId, out var _))
            _mind.TransferTo(mindId, uid.Comp.OriginalBody);
        var mind = Comp<MindComponent>(mindId);
        mind.PreventGhosting = false;
        QueueDel(uid);
        RemComp<CosmicMarkBlankComponent>(args.Performer);
    }
    #endregion

}
