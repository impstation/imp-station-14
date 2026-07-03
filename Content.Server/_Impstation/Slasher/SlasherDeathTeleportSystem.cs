using Content.Shared.Administration.Systems;
using Content.Shared.Mobs;
using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.Body.Systems;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Roles;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.Gibbing.Events;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;


namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Raised after failed-state Slasher death handling has completed.
/// </summary>
[ByRefEvent]
public record struct SlasherFailedDeathProcessedEvent;

/// <summary>
/// Handles Slasher death-maze teleportation, return-portal lifecycle, and owned-item reclaim.
/// All Slashers share the same configured death-maze grid when they die.
/// Procedural maze generation is delegated to <see cref="SlasherDeathMazeGeneratorSystem"/>.
/// </summary>
public sealed class SlasherDeathTeleportSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("slasher.deathmaze");

    private static readonly HashSet<string> CriticalStarterKitPrototypes =
    [
        "SlasherPDA",
    ];

    private static readonly Vector2i[] CardinalDirections =
    [
        new Vector2i(1, 0),
        new Vector2i(-1, 0),
        new Vector2i(0, 1),
        new Vector2i(0, -1),
    ];

    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SlasherDeathMazeGeneratorSystem _mazeGen = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Cached shared death-maze grid loaded once and reused for subsequent Slasher deaths.
    /// </summary>
    private EntityUid? _deathMazeGrid;

    /// <summary>
    /// Cached carved hallway tiles for the currently loaded death-maze grid.
    /// </summary>
    private HashSet<Vector2i>? _deathMazeHallwayTiles;

    /// <summary>
    /// The currently loaded death-maze grid, or null if none has been created yet.
    /// </summary>
    public EntityUid? DeathMazeGrid => _deathMazeGrid;

    /// <summary>
    /// Result payload for death-maze rift spawning.
    /// </summary>
    public readonly record struct DeathMazePortalSpawnResult(List<EntityCoordinates> PortalCoords, List<EntityUid> PortalEnts);

    /// <summary>
    /// Active return portals currently spawned in the death maze.
    /// </summary>
    private readonly List<EntityUid> _activeDeathMazePortals = new();

    /// <summary>
    /// Timestamp when return portals should spawn for the current death-maze visit.
    /// </summary>
    private TimeSpan? _portalSpawnTime;

    /// <summary>
    /// Subscribes Slasher mob-state transitions for death-maze teleport handling.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRoleComponent, MobStateChangedEvent>(OnMobStateChanged,
            after: [typeof(OnDeathEntitySpawnSystem)]);
        SubscribeLocalEvent<DeathMazeSpawnLocationComponent, EntityTerminatingEvent>(OnDeathMazeMarkerTerminating);
        SubscribeLocalEvent<MapGridComponent, EntityTerminatingEvent>(OnDeathMazeGridTerminating);
        SubscribeLocalEvent<SlasherRoleComponent, AttemptEntityGibCancelEvent>(OnAttemptGib);
        SubscribeLocalEvent<SlasherItemOwnershipComponent, EntityTerminatingEvent>(OnOwnedItemTerminating);
    }

    /// <summary>
    /// Processes death-maze portal lifecycle: delayed spawn while Slashers are present,
    /// and cleanup when no Slashers remain on the maze grid.
    /// </summary>
    /// <param name="frameTime">Elapsed frame time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_deathMazeGrid == null || !Exists(_deathMazeGrid.Value))
        {
            CleanupPortalState();
            return;
        }

        var hasSlashersInMaze = HasLivingSlashersInDeathMaze();

        if (_activeDeathMazePortals.Count > 0)
        {
            PrunePortalList();

            if (!hasSlashersInMaze)
            {
                DeleteActiveDeathMazePortals();
                _portalSpawnTime = null;
            }

            return;
        }

        if (!hasSlashersInMaze)
        {
            _portalSpawnTime = null;
            return;
        }

        if (!TryGetActiveRule(out var rule))
            return;

        _portalSpawnTime ??= _timing.CurTime + rule.Comp.DeathMazePortalSpawnDelay;
        if (_timing.CurTime < _portalSpawnTime.Value)
            return;

        var portals = SpawnDeathMazeReturnPortals();
        _activeDeathMazePortals.Clear();
        _activeDeathMazePortals.AddRange(portals.PortalEnts);
    }

    /// <summary>
    /// Clears the cached death-maze grid when one of its explicit spawn markers is deleted.
    /// </summary>
    private void OnDeathMazeMarkerTerminating(Entity<DeathMazeSpawnLocationComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_deathMazeGrid != null && Transform(ent).GridUid == _deathMazeGrid)
        {
            DeleteActiveDeathMazePortals();
            _deathMazeGrid = null;
            _deathMazeHallwayTiles = null;
            _portalSpawnTime = null;
        }
    }

    /// <summary>
    /// Clears the cached death-maze grid reference when the grid itself is deleted.
    /// </summary>
    private void OnDeathMazeGridTerminating(Entity<MapGridComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_deathMazeGrid == ent)
        {
            DeleteActiveDeathMazePortals();
            _deathMazeGrid = null;
            _deathMazeHallwayTiles = null;
            _portalSpawnTime = null;
        }
    }

    /// <summary>
    /// Teleports the Slasher to the death maze when they die and rejuvenates them.
    /// </summary>
    private void OnMobStateChanged(Entity<SlasherRoleComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;

        var hasMind = _mind.TryGetMind(ent.Owner, out var ownerMind, out _);

        if (HasComp<SlasherEffigyFailureComponent>(ent))
        {
            if (hasMind && TryGetDeathMazeSpawn(out var failureTarget))
                ReclaimOwnedItems(ent.Owner, ownerMind, failureTarget);

            _body.GibBody(ent.Owner, true);

            var ev = new SlasherFailedDeathProcessedEvent();
            RaiseLocalEvent(ent.Owner, ref ev);
            return;
        }

        if (!TryGetDeathMazeSpawn(out var target))
            return;

        if (hasMind)
            ReclaimOwnedItems(ent.Owner, ownerMind, target);

        _xform.SetCoordinates(ent.Owner, target);
        _rejuvenate.PerformRejuvenate(ent.Owner);

        // Second pass: catches items dropped by death systems that run synchronously before this handler.
        if (hasMind)
            ReclaimOwnedItems(ent.Owner, ownerMind, target);

        NotifySlasherEnteredDeathMaze();

        // Deferred third pass: catches items dropped by other MobStateChangedEvent subscribers that
        // run after this handler (e.g. hand-drop systems), which would otherwise leave gear on the station.
        var deferredMind = ownerMind;
        var deferredTarget = target;
        var deferredSlasher = ent.Owner;
        Timer.Spawn(0, () =>
        {
            if (TerminatingOrDeleted(deferredSlasher))
                return;
            ReclaimOwnedItems(deferredSlasher, deferredMind, deferredTarget);
        });
    }

    /// <summary>
    /// Prevents gibbing of Slashers unless they are already in effigy-failure state.
    /// </summary>
    private void OnAttemptGib(Entity<SlasherRoleComponent> ent, ref AttemptEntityGibCancelEvent args)
    {
        if (HasComp<SlasherEffigyFailureComponent>(ent.Owner))
            return; // Allow gibbing in failure state

        args.Cancelled = true;

        if (!TryGetDeathMazeSpawn(out var target))
            return;

        if (_mind.TryGetMind(ent.Owner, out var ownerMind, out _))
            ReclaimOwnedItems(ent.Owner, ownerMind, target);

        _xform.SetCoordinates(ent.Owner, target);
        _rejuvenate.PerformRejuvenate(ent.Owner);

        // Second pass: catches items dropped before this handler ran.
        if (_mind.TryGetMind(ent.Owner, out var refreshedMind, out _))
            ReclaimOwnedItems(ent.Owner, refreshedMind, target);

        NotifySlasherEnteredDeathMaze();

        // Deferred third pass: catches items dropped by gib-event subscribers running after this handler.
        var gibDeferredTarget = target;
        var gibDeferredSlasher = ent.Owner;
        Timer.Spawn(0, () =>
        {
            if (TerminatingOrDeleted(gibDeferredSlasher))
                return;
            if (_mind.TryGetMind(gibDeferredSlasher, out var lateMind, out _))
                ReclaimOwnedItems(gibDeferredSlasher, lateMind, gibDeferredTarget);
        });
    }

    /// <summary>
    /// Restores owned Slasher gear when a deletion source removes it from the world.
    /// </summary>
    private void OnOwnedItemTerminating(Entity<SlasherItemOwnershipComponent> ent, ref EntityTerminatingEvent args)
    {
        var prototypeId = MetaData(ent).EntityPrototype?.ID;
        if (prototypeId == null)
            return;

        if (!TryComp<MindComponent>(ent.Comp.OwnerMind, out var mind)
            || mind.OwnedEntity is not { } slasher
            || TerminatingOrDeleted(slasher))
        {
            return;
        }

        var replacement = Spawn(prototypeId, Transform(slasher).Coordinates);
        var replacementOwnership = EnsureComp<SlasherItemOwnershipComponent>(replacement);
        replacementOwnership.OwnerMind = ent.Comp.OwnerMind;
        replacementOwnership.Source = ent.Comp.Source;
        replacementOwnership.Slot = ent.Comp.Slot;

        _xform.DropNextTo(replacement, slasher);
    }

    /// <summary>
    /// Loads a fresh copy of the active rule's configured spawn shuttle for this Slasher
    /// and teleports them to a mapped marker.
    /// Called once per Slasher at round start so each has their own private shuttle.
    /// </summary>
    public bool TryTeleportToSpawnShuttle(EntityUid slasher)
    {
        if (!TryGetActiveRule(out var rule))
            return false;

        return TryTeleportToSpawnShuttle(slasher, rule.Comp.SpawnShuttlePath);
    }

    /// <summary>
    /// Loads a fresh copy of the specified spawn shuttle map and teleports the Slasher
    /// to its mapped shuttle spawn marker.
    /// </summary>
    /// <param name="slasher">The slasher entity to move.</param>
    /// <param name="shuttlePath">Shuttle map path to load.</param>
    /// <returns>True if the shuttle was loaded and the slasher was moved to a valid marker.</returns>
    public bool TryTeleportToSpawnShuttle(EntityUid slasher, Robust.Shared.Utility.ResPath shuttlePath)
    {
        if (shuttlePath == default)
            return false;

        _map.CreateMap(out var mapId);
        if (!_mapLoader.TryLoadGrid(mapId, shuttlePath, out var gridEnt))
            return false;

        var gridUid = gridEnt.Value.Owner;
        _map.SetPaused(mapId, false);
        if (!TryComp<MapGridComponent>(gridUid, out _))
            return false;

        if (!TryGetSpawnShuttleMarkerSpawn(gridUid, out var spawn) || !spawn.IsValid(EntityManager))
            return false;

        _xform.SetCoordinates(slasher, spawn);
        return true;
    }

    /// <summary>
    /// Resolves a spawn point in the shared death maze, creating it if needed.
    /// Spawn candidates must be walkable, non-space, non-blocked, and reachable from the maze center.
    /// </summary>
    /// <param name="destination">Resolved destination coordinates when successful.</param>
    /// <param name="selection">Whether to include center marker candidates, non-center candidates, or both.</param>
    /// <param name="avoid">Optional coordinate to avoid when selecting the destination.</param>
    public bool TryGetDeathMazeSpawn(
        out EntityCoordinates destination,
        DeathMazeSpawnSelection selection = DeathMazeSpawnSelection.Any,
        EntityCoordinates? avoid = null)
    {
        destination = EntityCoordinates.Invalid;

        if (!TryGetOrCreateDeathMazeGrid(out var gridUid, out var gridComp)
            || !TryGetActiveRule(out var rule))
        {
            return false;
        }

        if (!TryGetReachableDeathMazeTiles(gridUid, gridComp, rule.Comp, out var reachableTiles, out _, out var centerTile, out var validationFailureReason))
        {
            var success = TryGetEmergencyFallbackSpawn(gridUid, gridComp, selection, avoid, out destination);
            if (success)
                _sawmill.Warning($"Death-maze reachability validation failed ({validationFailureReason}). Using emergency fallback. Grid={gridUid}, Selection={selection}.");
            else
                _sawmill.Error($"Death-maze reachability validation failed ({validationFailureReason}) and emergency fallback also failed. Grid={gridUid}, Selection={selection}.");

            return success;
        }

        var sampledHallTiles = _mazeGen.GetSampledHallwayFallbackTiles(gridUid, gridComp, reachableTiles, centerTile, rule.Comp, _deathMazeHallwayTiles);
        var candidates = GetDeathMazeMarkerCandidates(gridUid, gridComp, selection, reachableTiles, centerTile, avoid);

        if (candidates.Count > 0)
        {
            destination = _random.Pick(candidates);
            return destination.IsValid(EntityManager);
        }

        return TryPickFallbackTile(gridUid, gridComp, reachableTiles, sampledHallTiles, centerTile, selection, avoid, out destination);
    }

    /// <summary>
    /// Returns a separated batch of death-maze spawn points for mass teleports.
    /// Selection prefers mapped marker points and falls back to reachable random floor tiles.
    /// </summary>
    /// <param name="desiredCount">Requested number of spawn coordinates.</param>
    /// <param name="selection">Whether to include center marker candidates, non-center candidates, or both.</param>
    /// <param name="minimumSeparation">Optional tile-distance separation override.</param>
    /// <param name="avoid">Optional coordinate to avoid in the selected set.</param>
    public List<EntityCoordinates> GetSeparatedDeathMazeSpawns(
        int desiredCount,
        DeathMazeSpawnSelection selection,
        int? minimumSeparation = null,
        EntityCoordinates? avoid = null)
    {
        var selected = new List<EntityCoordinates>();
        if (desiredCount <= 0)
            return selected;

        if (!TryGetOrCreateDeathMazeGrid(out var gridUid, out var gridComp)
            || !TryGetActiveRule(out var rule))
        {
            _sawmill.Warning($"Failed to resolve separated death-maze spawns. Grid={gridUid}, Count={desiredCount}, Selection={selection}.");
            return selected;
        }

        var separation = Math.Max(0, minimumSeparation ?? rule.Comp.DeathMazeSpawnMinSeparation);

        if (!TryGetReachableDeathMazeTiles(gridUid, gridComp, rule.Comp, out var reachableTiles, out _, out var centerTile, out var validationFailureReason))
        {
            // Mirror single-spawn behavior: if validation fails, still try to provide usable fallback points.
            _sawmill.Warning($"Separated death-maze spawn validation failed ({validationFailureReason}). Using emergency fallback batch. Grid={gridUid}, Count={desiredCount}, Selection={selection}.");

            var attempts = Math.Max(64, desiredCount * 16);
            for (var i = 0; i < attempts && selected.Count < desiredCount; i++)
            {
                var fallbackAvoid = selected.Count > 0 ? selected[^1] : avoid;
                if (!TryGetEmergencyFallbackSpawn(gridUid, gridComp, selection, fallbackAvoid, out var candidate))
                    continue;

                if (!candidate.IsValid(EntityManager))
                    continue;

                if (avoid is { } avoidCoords && AreSameTile(gridUid, gridComp, candidate, avoidCoords))
                    continue;

                var duplicate = false;
                foreach (var existing in selected)
                {
                    if (!AreSameTile(gridUid, gridComp, candidate, existing))
                        continue;

                    duplicate = true;
                    break;
                }

                if (duplicate)
                    continue;

                if (separation > 0 && IsTooCloseToAny(candidate, selected, separation))
                    continue;

                selected.Add(candidate);
            }

            return selected;
        }

        var candidates = GetDeathMazeMarkerCandidates(gridUid, gridComp, selection, reachableTiles, centerTile, avoid);
        var sampledHallTiles = _mazeGen.GetSampledHallwayFallbackTiles(gridUid, gridComp, reachableTiles, centerTile, rule.Comp, _deathMazeHallwayTiles);
        _random.Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (selected.Count >= desiredCount)
                break;

            if (separation > 0 && IsTooCloseToAny(candidate, selected, separation))
                continue;

            selected.Add(candidate);
        }

        if (selected.Count >= desiredCount)
            return selected;

        // Fill remaining slots from fallback tiles.
        var fallback = BuildFallbackTileList(gridUid, gridComp, reachableTiles, sampledHallTiles, centerTile, selection, avoid);
        _random.Shuffle(fallback);
        foreach (var candidate in fallback)
        {
            if (selected.Count >= desiredCount)
                break;

            if (selected.Contains(candidate))
                continue;

            if (separation > 0 && IsTooCloseToAny(candidate, selected, separation))
                continue;

            selected.Add(candidate);
        }

        return selected;
    }

    /// <summary>
    /// Resolves the canonical death-maze origin coordinate.
    /// Uses tile (0,0) when valid, otherwise falls back to the configured center marker.
    /// </summary>
    /// <param name="origin">Resolved origin coordinate when successful.</param>
    /// <returns>True when an origin coordinate could be resolved.</returns>
    public bool TryGetDeathMazeOriginCoordinates(out EntityCoordinates origin)
    {
        origin = EntityCoordinates.Invalid;

        if (!TryGetOrCreateDeathMazeGrid(out var gridUid, out var gridComp))
            return false;

        origin = _map.GridTileToLocal(gridUid, gridComp, new Vector2i(0, 0));
        if (origin.IsValid(EntityManager))
            return true;

        return TryGetDeathMazeCenter(gridUid, gridComp, out origin, out _);
    }

    /// <summary>
    /// Spawns return portals in the death maze.
    /// One portal is placed at the maze origin (0,0), and a configurable number are placed
    /// on accessible non-center tiles with separation.
    /// </summary>
    /// <returns>
    /// Spawn result containing both portal coordinates and portal entities that were created.
    /// </returns>
    public DeathMazePortalSpawnResult SpawnDeathMazeReturnPortals()
    {
        var portalCoords = new List<EntityCoordinates>();
        var portalEnts = new List<EntityUid>();

        if (!TryGetActiveRule(out var rule))
            return new DeathMazePortalSpawnResult(portalCoords, portalEnts);

        if (TryGetDeathMazeOriginCoordinates(out var origin)
            && origin.IsValid(EntityManager)
            && TrySpawnDeathMazePortal(rule.Comp.DeathMazePortalPrototype, origin, out var originPortal))
        {
            portalCoords.Add(origin);
            portalEnts.Add(originPortal);
        }

        var additional = Math.Max(0, rule.Comp.DeathMazeAdditionalPortalCount);
        if (additional <= 0)
            return new DeathMazePortalSpawnResult(portalCoords, portalEnts);

        EntityCoordinates? avoid = portalCoords.Count > 0 ? portalCoords[0] : null;
        var additionalCoords = GetSeparatedDeathMazeSpawns(additional, DeathMazeSpawnSelection.NonCenterOnly, avoid: avoid);
        foreach (var coord in additionalCoords)
        {
            if (!coord.IsValid(EntityManager))
                continue;

            if (!TrySpawnDeathMazePortal(rule.Comp.DeathMazePortalPrototype, coord, out var portal))
                continue;

            portalCoords.Add(coord);
            portalEnts.Add(portal);
        }

        return new DeathMazePortalSpawnResult(portalCoords, portalEnts);
    }

    /// <summary>
    /// Spawns a single death-maze portal prototype at the specified coordinate.
    /// </summary>
    /// <param name="prototype">Portal prototype to spawn.</param>
    /// <param name="coordinates">Spawn coordinate in the death maze.</param>
    /// <param name="portal">Spawned portal entity UID when successful.</param>
    /// <returns>True when the portal spawn succeeded.</returns>
    private bool TrySpawnDeathMazePortal(EntProtoId prototype, EntityCoordinates coordinates, out EntityUid portal)
    {
        portal = default;

        if (!coordinates.IsValid(EntityManager))
            return false;

        portal = Spawn(prototype, coordinates);
        return Exists(portal);
    }

    /// <summary>
    /// Starts the return-portal countdown when a Slasher enters the death maze.
    /// If a portal set is already active, this is a no-op.
    /// </summary>
    private void NotifySlasherEnteredDeathMaze()
    {
        if (_activeDeathMazePortals.Count > 0)
            return;

        if (!TryGetActiveRule(out var rule))
            return;

        _portalSpawnTime ??= _timing.CurTime + rule.Comp.DeathMazePortalSpawnDelay;
    }

    /// <summary>
    /// Returns true if at least one living Slasher is currently on the death-maze grid.
    /// </summary>
    /// <returns>True when a living Slasher is present on the death maze.</returns>
    private bool HasLivingSlashersInDeathMaze()
    {
        if (_deathMazeGrid == null)
            return false;

        var query = EntityQueryEnumerator<SlasherRoleComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var mob, out var xform))
        {
            if (mob.CurrentState != MobState.Alive)
                continue;

            if (xform.GridUid == _deathMazeGrid)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Deletes all active death-maze portal entities and clears tracking.
    /// </summary>
    private void DeleteActiveDeathMazePortals()
    {
        foreach (var portal in _activeDeathMazePortals)
        {
            if (Exists(portal))
                QueueDel(portal);
        }

        _activeDeathMazePortals.Clear();
    }

    /// <summary>
    /// Removes stale deleted entities from the tracked active portal list.
    /// </summary>
    private void PrunePortalList()
    {
        _activeDeathMazePortals.RemoveAll(uid => !Exists(uid));
    }

    /// <summary>
    /// Clears all return-portal runtime state.
    /// </summary>
    private void CleanupPortalState()
    {
        DeleteActiveDeathMazePortals();
        _portalSpawnTime = null;
    }

    /// <summary>
    /// Resolves the shared death-maze grid, creating and caching it on first use.
    /// </summary>
    private bool TryGetOrCreateDeathMazeGrid(out EntityUid gridUid, out MapGridComponent gridComp)
    {
        gridUid = default;
        gridComp = default!;

        if (!TryGetActiveRule(out var rule))
            return false;

        if (_deathMazeGrid is { } existing
            && Exists(existing)
            && TryComp<MapGridComponent>(existing, out var cached))
        {
            gridUid = existing;
            gridComp = cached;

            if (_deathMazeHallwayTiles == null
                && TryGetDeathMazeCenter(existing, cached, out _, out var centerTile))
            {
                _deathMazeHallwayTiles = _mazeGen.GenerateHallways(existing, cached, rule.Comp, centerTile);
            }

            return true;
        }

        if (!_mazeGen.TryCreateDeathMazeGrid(out var newGridUid, out var newGridComp))
            return false;

        if (TryGetDeathMazeCenter(newGridUid, newGridComp, out _, out var mazeCenter))
            _deathMazeHallwayTiles = _mazeGen.GenerateHallways(newGridUid, newGridComp, rule.Comp, mazeCenter);
        else
            _deathMazeHallwayTiles = null;

        _deathMazeGrid = newGridUid;
        gridUid = newGridUid;
        gridComp = newGridComp;
        return true;
    }

    /// <summary>
    /// Gathers death-maze marker spawn candidates filtered by selection criteria and reachability.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="selection">Filter mode: center markers, non-center markers, or all markers.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="avoid">Optional coordinate to exclude from candidates.</param>
    /// <returns>List of valid marker spawn coordinates matching the selection criteria.</returns>
    private List<EntityCoordinates> GetDeathMazeMarkerCandidates(
        EntityUid gridUid,
        MapGridComponent gridComp,
        DeathMazeSpawnSelection selection,
        HashSet<Vector2i> reachableTiles,
        Vector2i centerTile,
        EntityCoordinates? avoid)
    {
        var candidates = new List<EntityCoordinates>();

        var query = EntityQueryEnumerator<DeathMazeSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out _, out var marker, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Coordinates.IsValid(EntityManager))
                continue;

            if (selection == DeathMazeSpawnSelection.CenterOnly && !marker.IsCenter)
                continue;

            if (selection == DeathMazeSpawnSelection.NonCenterOnly && marker.IsCenter)
                continue;

            var tile = _map.CoordinatesToTile(gridUid, gridComp, xform.Coordinates);
            if (!reachableTiles.Contains(tile) || !_mazeGen.IsValidTile(gridUid, gridComp, tile))
                continue;

            if (selection == DeathMazeSpawnSelection.NonCenterOnly && tile == centerTile)
                continue;

            if (avoid is { } avoidCoords && AreSameTile(gridUid, gridComp, xform.Coordinates, avoidCoords))
                continue;

            candidates.Add(xform.Coordinates);
        }

        return candidates;
    }

    /// <summary>
    /// Attempts to pick a random fallback tile from valid hallway and reachable tiles.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="sampledHallTiles">Cached hallway tiles for spawn sampling.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="selection">Filter mode: center markers, non-center markers, or all markers.</param>
    /// <param name="avoid">Optional coordinate to exclude from selection.</param>
    /// <param name="destination">Resolved fallback coordinate when successful.</param>
    /// <returns>True if a valid fallback tile was found.</returns>
    private bool TryPickFallbackTile(
    EntityUid gridUid,
    MapGridComponent gridComp,
    HashSet<Vector2i> reachableTiles,
    HashSet<Vector2i> sampledHallTiles,
    Vector2i centerTile,
    DeathMazeSpawnSelection selection,
    EntityCoordinates? avoid,
    out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        var fallbackTiles = BuildFallbackTileList(gridUid, gridComp, reachableTiles, sampledHallTiles, centerTile, selection, avoid);
        if (fallbackTiles.Count == 0)
            return false;

        destination = _random.Pick(fallbackTiles);
        return destination.IsValid(EntityManager);
    }

    /// <summary>
    /// Builds a list of valid spawn candidates from hallway tiles and additional reachable tiles,
    /// excluding tiles that fail the selection filter.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="sampledHallTiles">Cached hallway tiles for spawn sampling.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="selection">Filter mode: center markers, non-center markers, or all markers.</param>
    /// <param name="avoid">Optional coordinate to exclude from the list.</param>
    /// <returns>List of local coordinates suitable for fallback spawn selection.</returns>
    private List<EntityCoordinates> BuildFallbackTileList(
    EntityUid gridUid,
    MapGridComponent gridComp,
    HashSet<Vector2i> reachableTiles,
    HashSet<Vector2i> sampledHallTiles,
    Vector2i centerTile,
    DeathMazeSpawnSelection selection,
    EntityCoordinates? avoid)
    {
        var fallback = new List<EntityCoordinates>(reachableTiles.Count);

        foreach (var tile in sampledHallTiles)
        {
            if (!PassesSelectionFilter(tile, centerTile, selection))
                continue;

            var local = _map.GridTileToLocal(gridUid, gridComp, tile);
            if (avoid is { } ac && AreSameTile(gridUid, gridComp, local, ac))
                continue;

            fallback.Add(local);
        }

        foreach (var tile in reachableTiles)
        {
            if (sampledHallTiles.Contains(tile))
                continue;

            if (!PassesSelectionFilter(tile, centerTile, selection))
                continue;

            var local = _map.GridTileToLocal(gridUid, gridComp, tile);
            if (avoid is { } ac && AreSameTile(gridUid, gridComp, local, ac))
                continue;

            fallback.Add(local);
        }

        return fallback;
    }

    /// <summary>
    /// Determines whether a tile passes the spawn selection criteria (center-only, non-center, or any).
    /// </summary>
    /// <param name="tile">The tile to test.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <param name="selection">The selection filter mode.</param>
    /// <returns>True if the tile matches the selection criteria.</returns>
    private static bool PassesSelectionFilter(Vector2i tile, Vector2i centerTile, DeathMazeSpawnSelection selection)
    {
        return selection switch
        {
            DeathMazeSpawnSelection.CenterOnly => tile == centerTile,
            DeathMazeSpawnSelection.NonCenterOnly => tile != centerTile,
            _ => true,
        };
    }

    /// <summary>
    /// Attempts to find a valid spawn point as a fallback when standard marker and tile logic fails,
    /// with support for additional validation rules.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="selection">Filter mode: center markers, non-center markers, or all markers.</param>
    /// <param name="avoid">Optional coordinate to exclude from selection.</param>
    /// <param name="destination">Resolved emergency fallback coordinate when successful.</param>
    /// <returns>True if an emergency fallback spawn point was found.</returns>
    private bool TryGetEmergencyFallbackSpawn(
        EntityUid gridUid,
        MapGridComponent gridComp,
        DeathMazeSpawnSelection selection,
        EntityCoordinates? avoid,
        out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        if (selection == DeathMazeSpawnSelection.CenterOnly
            && TryGetDeathMazeCenter(gridUid, gridComp, out var centerCoords, out var centerTile)
            && _mazeGen.IsValidTile(gridUid, gridComp, centerTile)
            && !(avoid is { } avoidCenter && AreSameTile(gridUid, gridComp, centerCoords, avoidCenter)))
        {
            destination = centerCoords;
            return true;
        }

        return TryFindRandomValidTile(gridUid, gridComp, out destination, 256, coords =>
        {
            if (avoid is { } avoidCoords && AreSameTile(gridUid, gridComp, coords, avoidCoords))
                return false;

            if (selection == DeathMazeSpawnSelection.Any)
                return true;

            var tile = _map.CoordinatesToTile(gridUid, gridComp, coords);
            if (!TryGetDeathMazeCenter(gridUid, gridComp, out _, out var cTile))
                return selection != DeathMazeSpawnSelection.CenterOnly;

            return PassesSelectionFilter(tile, cTile, selection);
        });
    }

    /// <summary>
    /// Builds reachable tile data from the maze center and validates minimum tile count,
    /// atmosphere safety, and power requirements per rule configuration.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="rule">The active Slasher rule component with validation requirements.</param>
    /// <param name="reachableTiles">Output set of tiles reachable from the maze center.</param>
    /// <param name="centerCoords">Output maze center coordinates.</param>
    /// <param name="centerTile">Output maze center tile coordinate.</param>
    /// <param name="failureReason">Output diagnostic message if validation fails.</param>
    /// <returns>True if the maze has sufficient reachable tiles and meets all validation criteria.</returns>
    private bool TryGetReachableDeathMazeTiles(
        EntityUid gridUid,
        MapGridComponent gridComp,
        SlasherRuleComponent rule,
        out HashSet<Vector2i> reachableTiles,
        out EntityCoordinates centerCoords,
        out Vector2i centerTile,
        out string failureReason)
    {
        reachableTiles = new HashSet<Vector2i>();
        centerCoords = EntityCoordinates.Invalid;
        centerTile = default;
        failureReason = "unknown validation failure";

        if (!TryGetDeathMazeCenter(gridUid, gridComp, out centerCoords, out centerTile)
            || !_mazeGen.IsValidTile(gridUid, gridComp, centerTile))
        {
            failureReason = $"center tile {centerTile} is invalid or unreachable";
            return false;
        }

        var queue = new Queue<Vector2i>();
        queue.Enqueue(centerTile);
        reachableTiles.Add(centerTile);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var direction in CardinalDirections)
            {
                var next = current + direction;
                if (!reachableTiles.Add(next))
                    continue;

                if (!_mazeGen.IsValidTile(gridUid, gridComp, next))
                {
                    reachableTiles.Remove(next);
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        if (reachableTiles.Count < rule.DeathMazeMinimumReachableTiles)
        {
            failureReason = $"only {reachableTiles.Count} reachable tiles found; minimum is {rule.DeathMazeMinimumReachableTiles}";
            return false;
        }

        if (!ValidateDeathMazeEnvironment(gridUid, reachableTiles, rule, out failureReason))
            return false;

        return true;
    }

    /// <summary>
    /// Validates atmosphere and power requirements for the death maze per rule configuration.
    /// Checks safe atmosphere tile count and powered light count against configured minimums.
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="reachableTiles">Set of tiles reachable from the maze center.</param>
    /// <param name="rule">The active Slasher rule component with validation requirements.</param>
    /// <param name="failureReason">Diagnostic message if validation fails.</param>
    /// <returns>True if the environment meets all configured validation criteria.</returns>
    private bool ValidateDeathMazeEnvironment(EntityUid gridUid, HashSet<Vector2i> reachableTiles, SlasherRuleComponent rule, out string failureReason)
    {
        failureReason = string.Empty;

        if (rule.RequireDeathMazeAtmosphere)
        {
            var safeTiles = 0;
            Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? gridAtmos = null;
            if (TryComp<GridAtmosphereComponent>(gridUid, out var atmosComp))
                gridAtmos = (gridUid, atmosComp, CompOrNull<GasTileOverlayComponent>(gridUid));

            foreach (var tile in reachableTiles)
            {
                var mixture = _atmosphere.GetTileMixture(gridAtmos, null, tile);
                if (_atmosphere.IsMixtureProbablySafe(mixture))
                    safeTiles++;
            }

            if (safeTiles < rule.DeathMazeMinimumSafeAtmosTiles)
            {
                failureReason = $"only {safeTiles} atmosphere-safe tiles; minimum is {rule.DeathMazeMinimumSafeAtmosTiles}";
                return false;
            }
        }

        if (rule.RequireDeathMazePower)
        {
            var poweredLights = 0;
            var lightQuery = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
            while (lightQuery.MoveNext(out var lightUid, out _, out var xform))
            {
                if (xform.GridUid == gridUid && this.IsPowered(lightUid, EntityManager))
                    poweredLights++;
            }

            if (poweredLights < rule.DeathMazeMinimumPoweredLights)
            {
                failureReason = $"only {poweredLights} powered lights found; minimum is {rule.DeathMazeMinimumPoweredLights}";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reclaims globally-owned Slasher items for the specified mind and moves them to the destination.
    /// Items already contained by the dying Slasher are left in place so they remain equipped/held.
    /// Base starter-kit items in slots covered by the active cosmetic variant are also left alone.
    /// </summary>
    /// <param name="slasher">The Slasher entity whose items are being reclaimed.</param>
    /// <param name="ownerMind">The mind entity that owns the gear.</param>
    /// <param name="destination">The coordinate where reclaimed items are placed.</param>
    private void ReclaimOwnedItems(EntityUid slasher, EntityUid ownerMind, EntityCoordinates destination)
    {
        if (!destination.IsValid(EntityManager))
            return;

        var suppressedSlots = new HashSet<string>();
        if (TryComp<SlasherCosmeticVariantComponent>(slasher, out var variantComp)
            && variantComp.SelectedVariantGearId != null
            && _proto.TryIndex<StartingGearPrototype>(variantComp.SelectedVariantGearId, out var variantGear))
        {
            foreach (var slot in variantGear.Equipment.Keys)
                suppressedSlots.Add(slot);
        }

        var query = EntityQueryEnumerator<SlasherItemOwnershipComponent>();
        while (query.MoveNext(out var uid, out var ownership))
        {
            if (ownership.OwnerMind != ownerMind || TerminatingOrDeleted(uid))
                continue;

            var prototypeId = MetaData(uid).EntityPrototype?.ID;
            var preserveThroughSuppression = prototypeId != null && CriticalStarterKitPrototypes.Contains(prototypeId);

            if (ownership.Source == SlasherOwnedItemSource.StarterKit
                && !preserveThroughSuppression
                && ownership.Slot != null
                && suppressedSlots.Contains(ownership.Slot))
                continue;

            if (IsContainedByEntity(uid, slasher))
                continue;

            _container.TryRemoveFromContainer(uid);
            _xform.SetCoordinates(uid, destination);
        }
    }

    /// <summary>
    /// Resolves the center position for death-maze pathing checks and validation.
    /// Uses a center marker when available; otherwise falls back to tile (0,0).
    /// </summary>
    /// <param name="gridUid">The death-maze grid entity.</param>
    /// <param name="gridComp">The death-maze grid component.</param>
    /// <param name="centerCoords">The maze center coordinates when located.</param>
    /// <param name="centerTile">The maze center tile coordinate.</param>
    /// <returns>True if a center coordinate could be determined.</returns>
    private bool TryGetDeathMazeCenter(EntityUid gridUid, MapGridComponent gridComp, out EntityCoordinates centerCoords, out Vector2i centerTile)
    {
        centerCoords = EntityCoordinates.Invalid;
        centerTile = new Vector2i(0, 0);

        var markerQuery = EntityQueryEnumerator<DeathMazeSpawnLocationComponent, TransformComponent>();
        while (markerQuery.MoveNext(out _, out var marker, out var xform))
        {
            if (!marker.IsCenter || xform.GridUid != gridUid || !xform.Coordinates.IsValid(EntityManager))
                continue;

            centerCoords = xform.Coordinates;
            centerTile = _map.CoordinatesToTile(gridUid, gridComp, xform.Coordinates);
            return true;
        }

        centerCoords = _map.GridTileToLocal(gridUid, gridComp, new Vector2i(0, 0));
        centerTile = new Vector2i(0, 0);
        return true;
    }

    /// <summary>
    /// Resolves the mapped spawn shuttle marker on the given shuttle grid.
    /// </summary>
    /// <param name="gridUid">The spawn shuttle grid entity.</param>
    /// <param name="destination">The mapped spawn shuttle marker coordinate when found.</param>
    /// <returns>True if a valid spawn shuttle marker exists on the grid.</returns>
    private bool TryGetSpawnShuttleMarkerSpawn(EntityUid gridUid, out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        var query = EntityQueryEnumerator<SlasherSpawnShuttleLocationComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == gridUid && xform.Coordinates.IsValid(EntityManager))
            {
                destination = xform.Coordinates;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Searches for a valid non-space, non-blocked spawn location via random tile sampling.
    /// </summary>
    /// <param name="gridUid">The grid entity to search.</param>
    /// <param name="grid">The grid component.</param>
    /// <param name="destination">A valid random spawn coordinate when successful.</param>
    /// <param name="attempts">Maximum number of random tiles to try before giving up.</param>
    /// <param name="additionalCheck">Optional predicate for custom validation rules beyond walkability.</param>
    /// <returns>True if a valid spawn tile was found within the attempt limit.</returns>
    private bool TryFindRandomValidTile(
        EntityUid gridUid,
        MapGridComponent grid,
        out EntityCoordinates destination,
        int attempts,
        Func<EntityCoordinates, bool>? additionalCheck = null)
    {
        destination = EntityCoordinates.Invalid;

        if (TerminatingOrDeleted(gridUid) || !Exists(gridUid))
            return false;

        var localAabb = grid.LocalAABB;
        var minX = (int)MathF.Floor(localAabb.Left);
        var maxX = (int)MathF.Ceiling(localAabb.Right);
        var minY = (int)MathF.Floor(localAabb.Bottom);
        var maxY = (int)MathF.Ceiling(localAabb.Top);

        if (minX >= maxX || minY >= maxY)
            return false;

        for (var i = 0; i < attempts; i++)
        {
            var tile = new Vector2i(_random.Next(minX, maxX), _random.Next(minY, maxY));
            if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef)
                || tileRef.Tile.IsEmpty
                || _turf.IsSpace(tileRef))
                continue;

            var localCoords = _map.GridTileToLocal(gridUid, grid, tile);
            if (!localCoords.IsValid(EntityManager))
                continue;

            if (additionalCheck != null && !additionalCheck(localCoords))
                continue;

            destination = localCoords;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tests whether an entity is directly or indirectly contained within another entity.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    /// <param name="potentialOwner">The container entity to check ownership against.</param>
    /// <returns>True if the entity is contained by the owner.</returns>
    private bool IsContainedByEntity(EntityUid entity, EntityUid potentialOwner)
    {
        var current = entity;
        var visited = new HashSet<EntityUid>();

        while (_container.TryGetContainingContainer(current, out var container))
        {
            if (!visited.Add(current))
                return false;

            if (container.Owner == potentialOwner)
                return true;

            current = container.Owner;
        }

        return false;
    }

    /// <summary>
    /// Returns true when two coordinates resolve to the same tile on the given grid.
    /// </summary>
    /// <param name="gridUid">The grid entity used for coordinate resolution.</param>
    /// <param name="gridComp">The grid component used for coordinate resolution.</param>
    /// <param name="a">First coordinate.</param>
    /// <param name="b">Second coordinate.</param>
    private bool AreSameTile(EntityUid gridUid, MapGridComponent gridComp, EntityCoordinates a, EntityCoordinates b)
    {
        if (!a.IsValid(EntityManager) || !b.IsValid(EntityManager))
            return false;

        return _map.CoordinatesToTile(gridUid, gridComp, a) == _map.CoordinatesToTile(gridUid, gridComp, b);
    }

    /// <summary>
    /// Returns true when a candidate coordinate is within <paramref name="minDistance"/> tiles of any already-selected coordinate.
    /// </summary>
    /// <param name="candidate">Coordinate to test.</param>
    /// <param name="selected">Already-selected coordinates to check against.</param>
    /// <param name="minDistance">Minimum required separation distance in tiles.</param>
    private bool IsTooCloseToAny(EntityCoordinates candidate, List<EntityCoordinates> selected, float minDistance)
    {
        foreach (var existing in selected)
        {
            if (candidate.TryDistance(EntityManager, existing, out var distance) && distance < minDistance)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the currently active Slasher game rule entity, if one exists.
    /// </summary>
    /// <param name="rule">The active rule entity when found.</param>
    /// <returns>True when an active Slasher rule is running.</returns>
    private bool TryGetActiveRule(out Entity<SlasherRuleComponent> rule)
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, SlasherRuleComponent>();
        if (query.MoveNext(out var uid, out _, out var comp))
        {
            rule = (uid, comp);
            return true;
        }

        rule = default;
        return false;
    }
}