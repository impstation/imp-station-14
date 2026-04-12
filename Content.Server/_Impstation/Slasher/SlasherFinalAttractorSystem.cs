using Content.Server._Impstation.Slasher.Components;
using Content.Server.Physics.Components;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Biases the SlasherFinalEntity's RandomWalk toward living crew members,
/// mirroring how SingularityAttractorSystem biases the singulo toward powered attractors.
/// The boss drifts organically but is pulled toward crowd density — "force of nature" feel.
/// </summary>
public sealed class SlasherFinalAttractorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Minimum distance before attraction is applied, to avoid division by zero.
    /// </summary>
    private const float MinAttractRange = 0.00001f;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherFinalAttractorComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Type definition for Update.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SlasherFinalEntityComponent, RandomWalkComponent, SlasherFinalAttractorComponent, TransformComponent>();
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
    /// Type definition for ShouldPulse.
    /// </summary>
    /// <param name="attractor">Parameter used by this method: attractor.</param>
    /// <param name="now">Parameter used by this method: now.</param>
    private static bool ShouldPulse(SlasherFinalAttractorComponent attractor, TimeSpan now)
    {
        return attractor.LastPulseTime + attractor.TargetPulsePeriod <= now;
    }

    /// <summary>
    /// Type definition for TryGetMapPosition.
    /// </summary>
    /// <param name="xform">Parameter used by this method: xform.</param>
    /// <param name="mapPos">Parameter used by this method: mapPos.</param>
    private bool TryGetMapPosition(TransformComponent xform, out MapCoordinates mapPos)
    {
        mapPos = _transform.ToMapCoordinates(xform.Coordinates);
        return mapPos != MapCoordinates.Nullspace;
    }

    /// <summary>
    /// Type definition for ApplyCrewAttraction.
    /// </summary>
    /// <param name="sourcePos">Parameter used by this method: sourcePos.</param>
    /// <param name="walk">Parameter used by this method: walk.</param>
    /// <param name="attractor">Parameter used by this method: attractor.</param>
    private void ApplyCrewAttraction(MapCoordinates sourcePos, RandomWalkComponent walk, SlasherFinalAttractorComponent attractor)
    {
        var nearby = new HashSet<Entity<HumanoidAppearanceComponent>>();
        _lookup.GetEntitiesInRange(sourcePos, attractor.MaxSearchRange, nearby);

        foreach (var crew in nearby)
        {
            if (!IsValidCrewTarget(crew))
                continue;

            var crewMapPos = _transform.ToMapCoordinates(Transform(crew).Coordinates);
            if (crewMapPos.MapId != sourcePos.MapId)
                continue;

            ApplyAttractionBias(sourcePos, crewMapPos, walk, attractor.BaseRange);
        }
    }

    /// <summary>
    /// Type definition for IsValidCrewTarget.
    /// </summary>
    /// <param name="crew">Parameter used by this method: crew.</param>
    private bool IsValidCrewTarget(EntityUid crew)
    {
        if (HasComp<SlasherRoleComponent>(crew))
            return false;

        return !TryComp<MobStateComponent>(crew, out var mobState) || mobState.CurrentState != MobState.Dead;
    }

    /// <summary>
    /// Type definition for ApplyAttractionBias.
    /// </summary>
    /// <param name="sourcePos">Parameter used by this method: sourcePos.</param>
    /// <param name="crewMapPos">Parameter used by this method: crewMapPos.</param>
    /// <param name="walk">Parameter used by this method: walk.</param>
    /// <param name="baseRange">Parameter used by this method: baseRange.</param>
    private static void ApplyAttractionBias(MapCoordinates sourcePos, MapCoordinates crewMapPos, RandomWalkComponent walk, float baseRange)
    {
        var biasBy = crewMapPos.Position - sourcePos.Position;
        var length = biasBy.Length();
        if (length <= MinAttractRange)
            return;

        // Inverse-distance scaled bias: closer crew pull more strongly.
        walk.BiasVector += Vector2.Normalize(biasBy) * (baseRange / length);
    }

    /// <summary>
    /// Type definition for OnMapInit.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnMapInit(Entity<SlasherFinalAttractorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastPulseTime = _timing.CurTime;
    }
}
