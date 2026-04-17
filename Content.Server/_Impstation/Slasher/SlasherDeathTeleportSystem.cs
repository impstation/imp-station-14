using Content.Shared.Coordinates.Helpers;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage.Components;
using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Server._Impstation.EntityEffects;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.Body.Systems;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Content.Shared.GameTicking.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Containers;
using Content.Shared.Gibbing.Events;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Raised after failed-state Slasher death handling has completed.
/// </summary>
[ByRefEvent]
public record struct SlasherFailedDeathProcessedEvent;

/// <summary>
/// Handles Slasher spawn-shuttle assignment at round start and death-maze teleportation on death.
/// Each Slasher spawns on their own fresh copy of the active rule's configured spawn shuttle.
/// All Slashers share the same configured death-maze grid when they die.
/// </summary>
public sealed class SlasherDeathTeleportSystem : EntitySystem
{
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

    /// <summary>
    /// Cached shared death-maze grid loaded once and reused for subsequent Slasher deaths.
    /// </summary>
    private EntityUid? _deathMazeGrid;

    /// <summary>
    /// The currently loaded death-maze grid, or null if none has been created yet.
    /// </summary>
    public EntityUid? DeathMazeGrid => _deathMazeGrid;

    /// <summary>
    /// Subscribes Slasher mob-state transitions for death-maze teleport handling.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRoleComponent, MobStateChangedEvent>(OnMobStateChanged,
            after: [typeof(OnDeathEntitySpawnSystem)]);
        SubscribeLocalEvent<SlasherRoleComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<DeathMazeSpawnLocationComponent, EntityTerminatingEvent>(OnDeathMazeMarkerTerminating);
        SubscribeLocalEvent<MapGridComponent, EntityTerminatingEvent>(OnDeathMazeGridTerminating);
        SubscribeLocalEvent<SlasherRoleComponent, AttemptEntityGibEvent>(OnAttemptGib);
    }

    /// <summary>
    /// Sends gibbed Slasher remains to the death maze right before the source body is deleted.
    /// </summary>
    /// <param name="ent">Slasher entity and role component.</param>
    /// <param name="args">Gib event payload containing spawned gib parts.</param>
    private void OnBeingGibbed(Entity<SlasherRoleComponent> ent, ref BeingGibbedEvent args)
    {
        // If the Slasher is in effigy-failure state, allow normal gibbing (do nothing).
        if (HasComp<SlasherEffigyFailureComponent>(ent.Owner))
            return;

        // Cancel gibbing: teleport and rejuvenate instead, then prevent gibs from spawning.
        if (!TryGetDeathMazeSpawn(out var target))
            return;

        if (_mind.TryGetMind(ent.Owner, out var ownerMind, out _))
            ReclaimOwnedItems(ent.Owner, ownerMind, target);

        _xform.SetCoordinates(ent.Owner, target);


        // Prevent gib parts from being spawned/handled.
        args.GibbedParts.Clear();
        _rejuvenate.PerformRejuvenate(ent.Owner); //they'll stlll dump all their blood on the floor but we can put that back
    }

    /// <summary>
    /// Type definition for OnDeathMazeMarkerTerminating.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnDeathMazeMarkerTerminating(Entity<DeathMazeSpawnLocationComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_deathMazeGrid != null && Transform(ent).GridUid == _deathMazeGrid)
            _deathMazeGrid = null;
    }

    /// <summary>
    /// Type definition for OnDeathMazeGridTerminating.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnDeathMazeGridTerminating(Entity<MapGridComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_deathMazeGrid == ent)
            _deathMazeGrid = null;
    }

    /// <summary>
    /// Teleports the slasher to the maints maze when they newly enter dead state and rejuvenates them.
    /// </summary>
    /// <param name="ent">Slasher entity and role component.</param>
    /// <param name="args">Mob state transition event data.</param>
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
    }

    /// <summary>
    /// Reclaims globally-owned Slasher items for the specified mind and moves them to the destination.
    /// Items already contained by the dying Slasher are left in place so they remain equipped/held.
    /// Base starter-kit items in slots covered by the active cosmetic variant are also left alone,
    /// since the cosmetic kit replacement takes ownership of those slots.
    /// </summary>
    /// <param name="slasher">Slasher entity whose items are being reclaimed.</param>
    /// <param name="ownerMind">Mind whose owned items should be reclaimed.</param>
    /// <param name="destination">Coordinates to which reclaimed items should be teleported.</param
    private void ReclaimOwnedItems(EntityUid slasher, EntityUid ownerMind, EntityCoordinates destination)
    {
        // Build the set of equipment slots suppressed by the active cosmetic variant.
        // Any base StarterKit item tagged with one of these slot names will not be reclaimed.
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

            // Skip base-kit items that belong to a slot now covered by the cosmetic variant.
            if (ownership.Source == SlasherOwnedItemSource.StarterKit
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
    /// IsConntainedByEntity checks whether the specified entity is contained (directly or indirectly) by the potential owner.
    /// </summary>
    /// <param name="entity">Entity to check for containment.</param>
    /// <param name="potentialOwner">Entity that may be an ancestor container of the target entity.</param>
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
    /// Loads a fresh copy of the active rule's configured spawn shuttle for this Slasher and teleports them to it.
    /// Called once per Slasher at round start so each has their own private shuttle.
    /// </summary>
    /// <param name="slasher">Slasher entity to place on their private spawn shuttle.</param>
    /// <returns>True when map load and spawn placement both succeed.</returns>
    public bool TryTeleportToSpawnShuttle(EntityUid slasher)
    {
        if (!TryGetActiveRule(out var rule))
            return false;

        _map.CreateMap(out var mapId);
        if (!_mapLoader.TryLoadGrid(mapId, rule.Comp.SpawnShuttlePath, out var gridEnt))
            return false;

        var gridUid = gridEnt.Value.Owner;
        _map.SetPaused(mapId, false);
        if (!TryComp<MapGridComponent>(gridUid, out var gridComp))
            return false;

        if (!TryFindRandomValidTile(gridUid, gridComp, out var spawn, rule.Comp.SpawnShuttleSearchAttempts))
            return false;

        _xform.SetCoordinates(slasher, spawn);
        return true;
    }

    /// <summary>
    /// Resolves a spawn point in the shared death maze, loading it if needed.
    /// Prefer explicit <see cref="DeathMazeSpawnLocationComponent"/> markers and fall back to random valid floor tiles.
    /// </summary>
    /// <param name="destination">Resolved destination coordinates when successful.</param>
    /// <returns>True when a valid death-maze destination is found.</returns>
    public bool TryGetDeathMazeSpawn(out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        if (!TryGetOrCreateDeathMazeGrid(out var gridUid, out var gridComp))
            return false;

        if (TryGetRandomDeathMazeMarkerSpawn(gridUid, out destination))
            return true;

        if (!TryGetActiveRule(out var rule))
            return false;

        return TryFindRandomValidTile(gridUid, gridComp, out destination, rule.Comp.DeathMazeSearchAttempts);
    }

    /// <summary>
    /// Prevents gibbing of Slashers unless in effigy-failure state. Teleports and rejuvenates instead.
    /// <param name="ent">Slasher entity and role component.</param>
    /// <param name="args">Gib event payload containing attempted gib type and count,
    /// </summary>
    private void OnAttemptGib(Entity<SlasherRoleComponent> ent, ref AttemptEntityGibEvent args)
    {
        if (HasComp<SlasherEffigyFailureComponent>(ent.Owner))
            return; // Allow gibbing in failure state

        args.Cancelled = true;

        // Teleport and rejuvenate as in OnBeingGibbed
        if (!TryGetDeathMazeSpawn(out var target))
            return;

        if (_mind.TryGetMind(ent.Owner, out var ownerMind, out _))
            ReclaimOwnedItems(ent.Owner, ownerMind, target);

        _xform.SetCoordinates(ent.Owner, target);
        _rejuvenate.PerformRejuvenate(ent.Owner);
    }

    /// <summary>
    /// Attempts to pick a random mapped <see cref="DeathMazeSpawnLocationComponent"/> on the death-maze grid.
    /// </summary>
    /// <param name="gridUid">Death-maze grid to search for marker entities.</param>
    /// <param name="destination">Resolved destination coordinates when successful.</param>
    /// <returns>True when at least one mapped marker exists on the target grid.</returns>
    private bool TryGetRandomDeathMazeMarkerSpawn(EntityUid gridUid, out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        var markers = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<DeathMazeSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == gridUid)
                markers.Add(xform.Coordinates);
        }

        if (markers.Count == 0)
            return false;

        destination = _random.Pick(markers);
        return true;
    }

    /// <summary>
    /// Resolves the shared death-maze grid, loading and caching it on first use.
    /// </summary>
    /// <param name="gridUid">Resolved death-maze grid entity when successful.</param>
    /// <param name="gridComp">Resolved map-grid component for the death maze.</param>
    /// <returns>True when a valid death-maze grid is available.</returns>
    private bool TryGetOrCreateDeathMazeGrid(out EntityUid gridUid, out MapGridComponent gridComp)
    {
        gridUid = default;
        gridComp = default!;

        if (_deathMazeGrid is { } existing
            && Exists(existing)
            && TryComp<MapGridComponent>(existing, out var cached))
        {
            gridUid = existing;
            gridComp = cached;
            return true;
        }

        if (!TryGetActiveRule(out var rule))
            return false;

        _map.CreateMap(out var mapId);
        if (!_mapLoader.TryLoadGrid(mapId, rule.Comp.DeathMazePath, out var loadedGridEnt))
            return false;

        var newGridUid = loadedGridEnt.Value.Owner;
        _map.SetPaused(mapId, false);
        if (!TryComp<MapGridComponent>(newGridUid, out var newGridComp))
            return false;

        _deathMazeGrid = newGridUid;
        gridUid = newGridUid;
        gridComp = newGridComp;
        return true;
    }

    /// <summary>
    /// Searches random tiles on the given grid for a valid non-space, non-blocked spawn location.
    /// </summary>
    /// <param name="gridUid">Grid entity containing candidate tiles.</param>
    /// <param name="grid">Grid component used for tile lookups.</param>
    /// <param name="destination">Resolved destination coordinates when successful.</param>
    /// <param name="attempts">Maximum random tile attempts.</param>
    /// <param name="additionalCheck">Optional extra validation predicate for each candidate coordinate.</param>
    /// <returns>True when a valid tile was found.</returns>
    private bool TryFindRandomValidTile(EntityUid gridUid,
        MapGridComponent grid,
        out EntityCoordinates destination,
        int attempts,
        Func<EntityCoordinates, bool>? additionalCheck = null)
    {
        destination = EntityCoordinates.Invalid;

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
                || _turf.IsSpace(tileRef)
                || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            var localCoords = _map.GridTileToLocal(gridUid, grid, tile).SnapToGrid();
            if (additionalCheck != null && !additionalCheck(localCoords))
                continue;

            destination = localCoords;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the currently active Slasher rule entity and component.
    /// </summary>
    /// <param name="rule">Active rule entity/component tuple when found.</param>
    /// <returns>True when an active Slasher rule exists.</returns>
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
