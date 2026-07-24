using Content.Server._Impstation.Slasher.Components;
using Content.Server.Physics.Components;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Feeds the SlasherFinalEntity's chase controller with the nearest living crew target.
/// Movement itself is handled by ChasingWalk, matching the same pursuit model used by the malicious knife.
/// </summary>
public sealed class SlasherFinalChaseTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherFinalChaseTargetComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Pulses active attractors and refreshes the boss chase target.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SlasherFinalEntityComponent, ChasingWalkComponent, SlasherFinalChaseTargetComponent, TransformComponent>();
        var now = _timing.CurTime;

        while (query.MoveNext(out _, out _, out var walk, out var attractor, out var xform))
        {
            if (!ShouldPulse(attractor, now))
                continue;

            attractor.LastPulseTime = now;

            if (!TryGetMapPosition(xform, out var mapPos))
                continue;

            ApplyCrewAttraction(mapPos, walk, attractor);
        }
    }

    /// <summary>
    /// Returns whether this attractor is ready to apply another attraction pulse.
    /// </summary>
    /// <param name="attractor">Attractor state to evaluate.</param>
    /// <param name="now">Current game time.</param>
    private static bool ShouldPulse(SlasherFinalChaseTargetComponent attractor, TimeSpan now)
    {
        return attractor.LastPulseTime + attractor.TargetPulsePeriod <= now;
    }

    /// <summary>
    /// Converts transform coordinates into map coordinates when the entity is not in nullspace.
    /// </summary>
    /// <param name="xform">Transform to convert.</param>
    /// <param name="mapPos">Resolved map coordinates when conversion succeeds.</param>
    private bool TryGetMapPosition(TransformComponent xform, out MapCoordinates mapPos)
    {
        mapPos = _transform.ToMapCoordinates(xform.Coordinates);
        return mapPos != MapCoordinates.Nullspace;
    }

    /// <summary>
    /// Scans for nearby crew and assigns the nearest valid chase target.
    /// </summary>
    /// <param name="sourcePos">Current map position of the boss.</param>
    /// <param name="walk">Chase controller to update.</param>
    /// <param name="targeter">Target-selection tuning values.</param>
    private void ApplyCrewAttraction(MapCoordinates sourcePos, ChasingWalkComponent walk, SlasherFinalChaseTargetComponent targeter)
    {
        var nearby = new HashSet<Entity<HumanoidAppearanceComponent>>();
        _lookup.GetEntitiesInRange(sourcePos, targeter.MaxSearchRange, nearby);

        EntityUid? nearestCrew = null;
        var nearestDistanceSquared = float.MaxValue;

        foreach (var crew in nearby)
        {
            if (!IsValidCrewTarget(crew))
                continue;

            var crewMapPos = _transform.ToMapCoordinates(Transform(crew).Coordinates);
            if (crewMapPos.MapId != sourcePos.MapId)
                continue;

            var distanceSquared = (crewMapPos.Position - sourcePos.Position).LengthSquared();
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearestCrew = crew;
        }

        walk.ChasingEntity = nearestCrew;
        if (nearestCrew != null)
            walk.Speed = walk.MaxSpeed;
    }

    /// <summary>
    /// Returns whether the candidate entity should be chased by the final boss.
    /// </summary>
    /// <param name="crew">Candidate crew entity.</param>
    private bool IsValidCrewTarget(EntityUid crew)
    {
        if (HasComp<SlasherRoleComponent>(crew))
            return false;

        return !TryComp<MobStateComponent>(crew, out var mobState) || mobState.CurrentState != MobState.Dead;
    }

    /// <summary>
    /// Seeds the pulse timer when the attractor entity initializes on a map.
    /// </summary>
    /// <param name="ent">Entity and attractor component data.</param>
    /// <param name="args">Map initialization event data.</param>
    private void OnMapInit(Entity<SlasherFinalChaseTargetComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastPulseTime = _timing.CurTime;
    }
}
