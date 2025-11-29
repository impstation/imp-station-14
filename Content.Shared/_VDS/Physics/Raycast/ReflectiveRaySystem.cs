using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._VDS.Physics;

public sealed partial class ReflectiveRaycastSystem : EntitySystem
{
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    [PublicAPI]
    public List<(RayResult probeResults, RayResult finalResults)> CastAndUpdateReflectiveRayStateRef(
        ref ReflectiveRayState state,
        int maxIterations,
        bool useRangeBudget = true)
    {
        var totalRange = 0f;
        var results = new List<(RayResult probeResults, RayResult finalResults)> { };
        for (var i = 0; i < maxIterations; i++)
        {
            results.Add(CastAndUpdateReflectiveRayStateRef(ref state));
            totalRange += state.CurrentSegmentDistance;
            if (useRangeBudget && totalRange >= state.MaxRange)
                break;
        }
        return results;
    }

    [PublicAPI]
    public (RayResult probeResult, RayResult pathResults) CastAndUpdateReflectiveRayStateRef(ref ReflectiveRayState state)
    {
        /*
            cast a probe ray until it finds the first entity that matches ProbeFilter
            note: _rayCast.CastRayClosest exists and you'd think it would be a better fit for a probe ray, but
            i don't know if i'm just using it wrong or if it's broken cause it seems to clip through walls
            if there is another grid behind that wall...
        */
        var probe = _rayCast.CastRay(
            state.MapId,
            state.CurrentPos,
            state.ProbeTranslation,
            state.ProbeFilter);
        if (TryUpdateStateToProbeHit(in probe, ref state))
        {
            var results = _rayCast.CastRay(state.MapId, state.OldPos, state.Translation, state.ResultsFilter);
#if DEBUG
            CastDebugRay(in state);
# endif
            UpdateStateReflect(ref state);
            return (probe, results);
        }
        else
        {
            var results = _rayCast.CastRay(state.MapId, state.OldPos, state.Translation, state.ResultsFilter);
            UpdateStateForward(ref state);
#if DEBUG
            CastDebugRay(in state);
# endif
            return (probe, results);
        }
    }

    [PublicAPI]
    public bool TryUpdateStateToProbeHit(in RayResult probeResult, ref ReflectiveRayState state)
    {
        if (probeResult.Hit)
        {
            UpdateStateToPos(ref state, probeResult.Results[0].Point);
            state.HitSurfaceNormal = Vector2.TransformNormal(
                probeResult.Results[0].LocalNormal,
                _transformSystem.GetWorldMatrix(probeResult.Results[0].Entity));

            return true;
        }

        return false;
    }

    [PublicAPI]
    public static void UpdateStateForward(ref ReflectiveRayState state)
    {
        state.OldPos = state.CurrentPos;

        // set our new position with our translation
        state.CurrentPos = state.OldPos + state.Translation;

        state.CurrentSegmentDistance = MathF.Max(0.05f, Vector2.Distance(state.OldPos, state.CurrentPos));
        state.RemainingDistance -= state.CurrentSegmentDistance;

        state.Translation = state.Direction * state.CurrentSegmentDistance;
    }

    [PublicAPI]
    public static void UpdateStateToPos(ref ReflectiveRayState state, in Vector2 worldHitPos)
    {
        // update our old position to be the previous one
        state.OldPos = state.CurrentPos;
        // set our new position at the hit entity
        state.CurrentPos = worldHitPos;

        // calculate the distance between our updated points. we use mathf.max so we never get 0 and explode
        state.CurrentSegmentDistance = MathF.Max(0f, Vector2.Distance(state.OldPos, state.CurrentPos));
        state.RemainingDistance -= state.CurrentSegmentDistance;

        state.Translation = state.Direction * state.CurrentSegmentDistance;
        state.ProbeTranslation = state.Direction * state.MaxRange;
    }

    [PublicAPI]
    public static void UpdateStateReflect(ref ReflectiveRayState state)
    {
        DebugTools.AssertNotNull(state.HitSurfaceNormal, "Can't reflect current raycast without a surface normal!");

        state.CurrentPos += state.HitSurfaceNormal!.Value * 0.05f;

        // boing
        state.Direction = Vector2.Normalize(Vector2.Reflect(state.Direction, state.HitSurfaceNormal.Value));

        // gas
        state.Translation = state.Direction * state.CurrentSegmentDistance;
        state.ProbeTranslation = state.Direction * state.MaxRange;
    }
}

public ref struct ReflectiveRayState(
    QueryFilter probeFilter,
    QueryFilter pathFilter,
    Vector2 origin,
    Vector2 direction,
    float maxRange,
    MapId mapId
    )
{
    public QueryFilter ProbeFilter = probeFilter;
    public QueryFilter ResultsFilter = pathFilter;
    public Vector2 CurrentPos = origin;
    public Vector2 OldPos;
    public Vector2 Direction = direction;
    public float MaxRange = maxRange;
    public float CurrentSegmentDistance = maxRange;
    public float RemainingDistance = maxRange;
    public MapId MapId = mapId;
    public Vector2 Translation = direction * maxRange;
    public Vector2 ProbeTranslation = direction * maxRange;
    public Vector2? HitSurfaceNormal = null;
}
