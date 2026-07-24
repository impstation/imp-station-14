using Content.Server._Impstation.Slasher.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles collision-based teleport behavior for Slasher rifts.
/// Trigger model mirrors standard portal collision handling.
/// </summary>
public sealed class SlasherRiftTeleportSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <summary>
    /// Subscribes portal-style collision enter/exit handlers for rift entities.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherRiftTeleportComponent, StartCollideEvent>(OnStartCollide);
    }

    /// <summary>
    /// Teleports entities that collide with the rift's portal fixture.
    /// </summary>
    /// <param name="ent">Rift entity and component data.</param>
    /// <param name="args">Collision-start event data.</param>
    private void OnStartCollide(Entity<SlasherRiftTeleportComponent> ent, ref StartCollideEvent args)
    {
        if (!ShouldCollide(ent.Comp, args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var subject = args.OtherEntity;
        if (Transform(subject).Anchored)
            return;

        if (!TryGetMidroundOrVentDestination(ent, subject, out var target))
            return;

        if (!target.IsValid(EntityManager))
            return;

        _xform.SetCoordinates(subject, target);
    }

    /// <summary>
    /// Resolves destination by station context: midround spawn first, vent spawn fallback.
    /// </summary>
    /// <param name="rift">Rift entity used to resolve station context.</param>
    /// <param name="subject">Entity being teleported.</param>
    /// <param name="target">Resolved destination coordinates when successful.</param>
    /// <returns>True when a destination was found.</returns>
    private bool TryGetMidroundOrVentDestination(EntityUid rift, EntityUid subject, out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var station = _station.GetOwningStation(rift) ?? _station.GetOwningStation(subject);
        if (station != null)
        {
            if (TryGetRandomMidroundAntagSpawn(station.Value, out target))
                return true;

            if (TryGetRandomVentFallbackSpawn(station.Value, out target))
                return true;
        }

        // Death-maze and private shuttle maps may have no owning station. Fall back to global mapped spawns.
        if (TryGetRandomGlobalMidroundAntagSpawn(out target))
            return true;

        return TryGetRandomGlobalVentFallbackSpawn(out target);
    }

    /// <summary>
    /// Picks a random mapped MidRoundAntag spawn location on the target station.
    /// </summary>
    /// <param name="station">Station to query for spawn markers.</param>
    /// <param name="target">Resolved destination coordinates when successful.</param>
    /// <returns>True when at least one mapped midround spawn exists.</returns>
    private bool TryGetRandomMidroundAntagSpawn(EntityUid station, out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<MidRoundAntagSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (_station.GetOwningStation(uid, xform) == station
                && xform.GridUid != null
                && xform.Coordinates.IsValid(EntityManager))
                spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
            return false;

        target = _random.Pick(spawns);
        return true;
    }

    /// <summary>
    /// Falls back to random mapped vent spawn locations on the target station.
    /// </summary>
    /// <param name="station">Station to query for vent fallback markers.</param>
    /// <param name="target">Resolved destination coordinates when successful.</param>
    /// <returns>True when at least one mapped vent fallback spawn exists.</returns>
    private bool TryGetRandomVentFallbackSpawn(EntityUid station, out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (_station.GetOwningStation(uid, xform) == station
                && xform.GridUid != null
                && xform.Coordinates.IsValid(EntityManager))
                spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
            return false;

        target = _random.Pick(spawns);
        return true;
    }

    /// <summary>
    /// Picks a random mapped MidRoundAntag spawn location globally, regardless of station ownership.
    /// </summary>
    /// <param name="target">Resolved destination coordinates when successful.</param>
    /// <returns>True when at least one mapped midround spawn exists globally.</returns>
    private bool TryGetRandomGlobalMidroundAntagSpawn(out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<MidRoundAntagSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != null && xform.Coordinates.IsValid(EntityManager))
                spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
            return false;

        target = _random.Pick(spawns);
        return true;
    }

    /// <summary>
    /// Falls back to random mapped vent spawn locations globally, regardless of station ownership.
    /// </summary>
    /// <param name="target">Resolved destination coordinates when successful.</param>
    /// <returns>True when at least one mapped vent fallback spawn exists globally.</returns>
    private bool TryGetRandomGlobalVentFallbackSpawn(out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != null && xform.Coordinates.IsValid(EntityManager))
                spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
            return false;

        target = _random.Pick(spawns);
        return true;
    }

    /// <summary>
    /// Uses portal-style fixture filtering so behavior matches existing portal triggers.
    /// </summary>
    /// <param name="ourId">Fixture ID on the rift entity.</param>
    /// <param name="otherId">Fixture ID on the colliding entity.</param>
    /// <param name="our">Rift fixture data.</param>
    /// <param name="other">Colliding fixture data.</param>
    /// <returns>True when the collision should trigger teleport handling.</returns>
    private static bool ShouldCollide(SlasherRiftTeleportComponent component, string ourId, string otherId, Fixture other)
    {
        return ourId == component.PortalFixtureId && (other.Hard || otherId == component.ProjectileFixtureId);
    }
}
