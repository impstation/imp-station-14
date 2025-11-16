// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Physics;
using Content.Shared._Mono.CCVar;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Client.Player;
using Content.Shared._VDS.Audio;
using Robust.Shared.Debugging;
using Content.Shared.Coordinates;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Content.Shared.Maps;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;

namespace Content.Client._Mono.Audio;

/// <summary>
/// Spawns bouncing rays from the player, for the purposes of acoustics.
/// </summary>
public sealed class AreaEchoSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly AudioEffectSystem _audioEffectSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedDebugRayDrawingSystem _debugRay = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roofSystem = default!;

    /// <summary>
    ///     The directions that are raycasted to determine size for echo.
    ///         Used relative to the grid.
    /// </summary>
    private Angle[] _calculatedDirections = [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];

    /// <summary>
    ///     Values for the minimum arbitrary size at which a certain audio preset
    ///         is picked for sounds. The higher the highest distance here is,
    ///         the generally more calculations it has to do.
    /// </summary>
    /// <remarks>
    ///     Keep in ascending order.
    /// </remarks>
    private static readonly AudioDistanceThreshold[] DistancePresets =
    [
        new(18f, "Hallway"),
        new(30f, "Auditorium"),
        new(45f, "ConcertHall"),
        new(50f, "Hangar")
    ];

    private readonly float _minimumMagnitude = DistancePresets[0].Distance;
    private readonly float _maximumMagnitude = DistancePresets[^1].Distance;
    private float _prevAvgMagnitude;

    /// <summary>
    ///     The client's local entity.
    /// </summary>
    private EntityUid _clientEnt;

    /// <summary>
    ///     When is the next time we should check all audio entities and see if they are eligible to be updated.
    /// </summary>
    private TimeSpan _nextExistingUpdate = TimeSpan.Zero;

    private int _echoMaxReflections;
    private bool _echoEnabled = true;

    /// <summary>
    /// How often we should check existing audio re-apply or remove echo from them when necessary.
    /// </summary>
    private TimeSpan _calculationInterval = TimeSpan.FromSeconds(15);

    private EntityQuery<AudioAbsorptionComponent> _absorptionQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<RoofComponent> _roofQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoReflectionCount, x => _echoMaxReflections = x, invokeImmediately: true);

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoEnabled, x => _echoEnabled = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(MonoCVars.AreaEchoHighResolution, x => _calculatedDirections = GetEffectiveDirections(x), invokeImmediately: true);

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoRecalculationInterval, x => _calculationInterval = x, invokeImmediately: true);

        _absorptionQuery = GetEntityQuery<AudioAbsorptionComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<AudioComponent, EntParentChangedMessage>(OnAudioParentChanged);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_echoEnabled ||
            _gameTiming.CurTime < _nextExistingUpdate)
            return;

        _nextExistingUpdate = _gameTiming.CurTime + _calculationInterval;
        var audioEnumerator = EntityQueryEnumerator<AudioComponent>();

        while (audioEnumerator.MoveNext(out var uid, out var audioComponent))
        {
            if (!CanAudioEcho(audioComponent) ||
                !audioComponent.Playing)
                continue;

            ProcessAudioEntity((uid, audioComponent));
        }
    }

    /// <summary>
    ///     Returns the appropiate DistantPreset, or the largest if somehow it can't be found.
    /// </summary>
    [Pure]
    public static ProtoId<AudioPresetPrototype> GetBestPreset(float magnitude)
    {
        foreach (var preset in DistancePresets)
        {
            if (preset.Distance >= magnitude)
                return preset.Preset;
        }

        // fallback to largest preset
        return DistancePresets[^1].Preset;
    }

    /// <summary>
    ///     Returns all four cardinal directions when <paramref name="highResolution"/> is false.
    ///         Otherwise, returns all eight intercardinal and cardinal directions as listed in
    ///         <see cref="DirectionExtensions.AllDirections"/>.
    /// </summary>
    [Pure]
    public static Angle[] GetEffectiveDirections(bool highResolution)
    {
        if (highResolution)
        {
            var allDirections = DirectionExtensions.AllDirections;
            var directions = new Angle[allDirections.Length];

            for (var i = 0; i < allDirections.Length; i++)
                directions[i] = allDirections[i].ToAngle();

            return directions;
        }

        return [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];
    }

    // /// <summary>
    // ///     Takes an entity's <see cref="TransformComponent"/>. Goes through every parent it
    // ///         has before reaching one that is a map. Returns the hierarchy
    // ///         discovered, which includes the given <paramref name="originEntity"/>.
    // /// </summary>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private List<Entity<TransformComponent>> TryGetHierarchyBeforeMap(Entity<TransformComponent> originEntity)
    // {
    //     var hierarchy = new List<Entity<TransformComponent>>() { originEntity };
    //
    //     ref var currentEntity = ref originEntity;
    //     ref var currentTransformComponent = ref currentEntity.Comp;
    //
    //     var mapUid = currentEntity.Comp.MapUid;
    //
    //     while (currentTransformComponent.ParentUid != mapUid /* break when the next entity is a map... */ &&
    //         currentTransformComponent.ParentUid.IsValid() /* ...or invalid */ )
    //     {
    //         // iterate to next entity
    //         var nextUid = currentTransformComponent.ParentUid;
    //         currentEntity.Owner = nextUid;
    //         currentTransformComponent = Transform(nextUid);
    //
    //         hierarchy.Add(currentEntity);
    //     }
    //
    //     DebugTools.Assert(hierarchy.Count >= 1, "Malformed entity hierarchy! Hierarchy must always contain one element, but it doesn't. How did this happen?");
    //     return hierarchy;
    // }

    /// <summary>
    ///     Basic check for whether an audio can echo. Doesn't account for distance.
    /// </summary>
    public bool CanAudioEcho(AudioComponent audioComponent)
    {
        return !audioComponent.Global && _echoEnabled;
    }

    /// <summary>
    ///     Gets the length of the direction that reaches the furthest unobstructed
    ///         distance, in an attempt to get the size of the area. Aborts early
    ///         if either grid is missing or the tile isnt rooved.
    ///
    ///     Returned magnitude is the longest valid length of the ray in each direction,
    ///         divided by the number of total processed angles.
    /// </summary>
    /// <returns>Whether anything was actually processed.</returns>
    // i am the total overengineering guy... and this, is my code.
    /*
        This works under a few assumptions:
        - An entity in space is invalid
        - Any spaced tile is invalid
        - Rays end on invalid tiles (space) or unrooved tiles, and dont process on separate grids.
        - - This checked every `_calculationalFidelity`-ish tiles. Not precisely. But somewhere around that. Its moreso just proportional to that.
        - Rays bounce.
    */
    public bool TryProcessAreaSpaceMagnitude(EntityUid clientEnt, float maximumMagnitude, out float magnitude)
    {
        magnitude = 0f;
        if (!clientEnt.IsValid() ||
            !_transformQuery.HasComponent(clientEnt)
            )
        {
            Logger.Warning($"fuck. {clientEnt.IsValid()} client ent: {clientEnt.Id}, ");
            return false;
        }
        var clientTransform = Transform(clientEnt);
        var clientMapId = clientTransform.MapID;
        var clientCoords = _transformSystem.ToMapCoordinates(clientTransform.Coordinates).Position;

        if (!_turfSystem.TryGetTileRef(clientEnt.ToCoordinates(), out var tileRef)
            || _turfSystem.IsSpace(tileRef.Value))
        {
            return false;
        }

        var environmentResults = new List<EchoRayStats>(_calculatedDirections.Length);

        var filter = new QueryFilter
        {
            MaskBits = (int)CollisionGroup.DoorPassable,
            IsIgnored = ent => !_absorptionQuery.HasComp(ent),
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };
        var stopAtFilter = new QueryFilter
        {
            LayerBits = (int)CollisionGroup.WallLayer,
            IsIgnored = ent => _absorptionQuery.TryGetComponent(ent, out var comp) && comp.ReflectRay,
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };

        foreach (var direction in _calculatedDirections)
        {
            var rand = _random.NextFloat(-1f, 1f);
            var offsetDirection = direction + rand;
            CastAudioRay(
                    stopAtFilter,
                    filter,
                    clientMapId, clientCoords, offsetDirection.ToVec(),
                    maximumMagnitude, _echoMaxReflections, maximumMagnitude,
                    out var stats);

            environmentResults.Add(stats);
        }

        if (environmentResults.Count == 0)
        {
            return false;
        }

        var totalRays = _calculatedDirections.Length;
        var avgMagnitude = environmentResults.Average(mag => mag.Magnitude);
        var avgAbsorption = environmentResults.Average(absorb => absorb.TotalAbsorption);
        var avgBounces = (float)environmentResults.Average(bounce => bounce.TotalBounces);
        var avgEscaped = (float)environmentResults.Average(escapees => escapees.TotalEscapes);

        if (_prevAvgMagnitude > float.Epsilon)
            avgMagnitude = MathHelper.Lerp(_prevAvgMagnitude, avgMagnitude, 0.25f);
        _prevAvgMagnitude = avgMagnitude;



        var finalMagnitude = 0f;
        finalMagnitude += avgMagnitude;
        finalMagnitude *= InverseNormalizeToPercentage(avgAbsorption, 100f);
        finalMagnitude *= InverseNormalizeToPercentage(avgEscaped, 100f);

        if (clientTransform.GridUid.HasValue
            && _roofQuery.TryGetComponent(clientTransform.GridUid.Value, out var roof)
            && _gridQuery.TryGetComponent(clientTransform.GridUid.Value, out var grid)
            && _transformSystem.TryGetGridTilePosition(clientEnt, out var indices)
            && !_roofSystem.IsRooved((clientTransform.GridUid.Value, grid, roof), indices))
        {
            Logger.Debug("reached");
            finalMagnitude *= 0.3f;
        }

        magnitude = finalMagnitude;
        Logger.Debug($"""
                Acoustics:
                - Average Magnitude: {avgMagnitude:F2}
                - Average Absorption: {avgAbsorption:F2}
                - Average Escaped: {avgEscaped:F2}
                - Average Bounces: {avgBounces:F2}

                - Absorb Coefficient: {InverseNormalizeToPercentage(avgAbsorption, 100f):F2}
                - Escape Coefficient: {InverseNormalizeToPercentage(avgEscaped, 100f):F2}
                - Final Magnitude: {magnitude}
                - Preset: {GetBestPreset(magnitude)}

                """);

        return true;
    }

    /// <summary>
    ///     Returns an epsilon..1.0f percent, where the closer to 0 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    private static float InverseNormalizeToPercentage(float value, float total)
    {
        return MathF.Max((total - value) / total * 1f, 0.01f);
    }

    /// <summary>
    ///     Returns an epsilon..1.0f percent, where the closer to 1 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    private static float NormalizeToPercentage(float value, float total)
    {
        return MathF.Max(value / total * 1f, 0.01f);
    }

    private void CastAudioRay(
        QueryFilter stopAtFilter, QueryFilter filter, MapId mapId, Vector2 startPos,
        Vector2 direction, float maxDistance, int maxIterations, float maxProbeLength,
        out EchoRayStats rayStats)
    {
        var currentDirection = Vector2.Normalize(direction);
        var translation = currentDirection * maxDistance;
        var probeTranslation = currentDirection * maxProbeLength;

        var stepData = new EchoRayStep
        {
            OldPos = startPos,
            NewPos = startPos,
            TotalDistance = 0f,
            RemainingDistance = maxDistance,
            MaxProbeDistance = maxProbeLength,
            Direction = currentDirection,
            Translation = translation,
            ProbeTranslation = probeTranslation,

        };

        rayStats = new EchoRayStats
        {
            TotalAbsorption = 0f,
            TotalBounces = 0,
            TotalEscapes = 0,
            Magnitude = 0
        };

        // time to start casting
        for (var iteration = 0; iteration <= maxIterations; iteration++)
        {
            Vector2? worldNormal = null;

            /*
                cast a probe ray to find nearest solid wall. notice the filter.
                note: _rayCast.CastRayClosest exists and you'd think it would be a better fit for a probe ray, but
                i don't know if i'm just using it wrong or if it's broken cause it seems to clip through walls
                if there is another grid behind that wall...
            */
            var probe = _rayCast.CastRay(mapId, stepData.NewPos, stepData.ProbeTranslation, stopAtFilter);
            if (probe.Results.Count > 0)
            {
                Logger.Debug($"HIT: {ToPrettyString(probe.Results[0].Entity)}");
                var worldMatrix = _transformSystem.GetWorldMatrix(probe.Results[0].Entity);
                var mapHitPos = probe.Results[0].Point;

                worldNormal = Vector2.TransformNormal(probe.Results[0].LocalNormal, worldMatrix);
                worldNormal = Vector2.Normalize(worldNormal.Value);


                UpdateProbeStep(ref stepData, mapHitPos);
                UpdateAcousticData(ref rayStats, probe.Results[0], stepData.NewDistance, _clientEnt);
            }
#if DEBUG
            // jank as fuck but whatever
            _debugRay.ReceiveLocalRayFromAnyThread(new(
                Ray: new Ray(stepData.OldPos, stepData.Direction),
                MaxLength: stepData.NewDistance,
                Results: null,
                ServerSide: false,
                mapId));
#endif
            // cast our results ray that'll go to the wall we found with our probe- if any. scans for acoustic data.
            var results = _rayCast.CastRay(mapId, stepData.OldPos, stepData.Translation, filter);
            if (results.Results.Count > 0)
            {
                // go through all hit entities and add up their data
                foreach (var hit in results.Results)
                {
                    UpdateAcousticData(ref rayStats, hit, stepData.NewDistance, _clientEnt);
                }
            }

            // now we can do our bounce
            if (worldNormal.HasValue)
            {
                UpdateProbeStepReflect(ref stepData, worldNormal.Value);
                rayStats.TotalBounces++;
            }
            else
            {
                // or keep movin.
                UpdateStepForward(ref stepData);
            }

            // consider our ray escaped into an open enough room/space if it traveled far
            if (stepData.NewDistance > maxDistance * 0.45)
            {
                rayStats.Magnitude = stepData.TotalDistance;
                rayStats.TotalEscapes++;
                break;
            }

            // back to start with our new step data
            rayStats.Magnitude = stepData.TotalDistance;

            // unless we're out of budget, or our positions are too close (indicating we're stuck)
            if (stepData.RemainingDistance <= 0)
            {
                break;
            }
        }
    }

    private void UpdateAcousticData(ref EchoRayStats stats, in RayHit hit, in float maxDistance, in EntityUid listener)
    {
        if (_absorptionQuery.TryGetComponent(hit.Entity, out var comp))
        {
            /*
                more type of data could be added in the future.
                instead of just a pure absorption value you could have
                material type and stuff and do whatever with that.
                that's why this is a method. for easy editing in the future.
            */

            Logger.Debug($"FOUND: {ToPrettyString(hit.Entity)}, absorption: {comp.Absorption}");

            // linear decay based on distance from the listener and the final ray distance.
            hit.Entity.ToCoordinates().TryDistance(
                    EntityManager,
                    listener.ToCoordinates(),
                    out var distance
                    );
            var distanceFactor = MathHelper.Clamp(1f - (distance - maxDistance) / maxDistance, 0f, 100f);
            stats.TotalAbsorption += comp.Absorption * distanceFactor;
            Logger.Debug($"New Total Absorb {stats.TotalAbsorption}");
        }
    }

    private static void UpdateProbeStep(ref EchoRayStep step, in Vector2 worldHitPos)
    {
        // update our old position to be the previous new one
        step.OldPos = step.NewPos;
        // set our new position at the hit entity (slightly offset from its normal to prevent clipping)
        step.NewPos = worldHitPos;

        // math magic or something
        // calculate the distance between our updated points
        step.NewDistance = Vector2.Distance(step.OldPos, step.NewPos);
        step.NewDistance = MathF.Max(0f, step.NewDistance); // floating point my belothed

        step.RemainingDistance -= step.NewDistance;
        step.TotalDistance += step.NewDistance;

        // convert our direction into a translation for the results ray.
        step.Translation = step.Direction * step.NewDistance;
        step.ProbeTranslation = step.Direction * step.MaxProbeDistance;
    }

    private static void UpdateProbeStepReflect(ref EchoRayStep step, in Vector2 worldNormal)
    {
        step.NewPos += worldNormal * 0.05f;
        step.OldPos = step.NewPos;

        // boing
        step.Direction = Vector2.Reflect(step.Direction, worldNormal);
        step.Direction = Vector2.Normalize(step.Direction);

        // gas
        step.Translation = step.Direction * step.NewDistance;
        step.ProbeTranslation = step.Direction * step.MaxProbeDistance;
    }

    private static void UpdateStepForward(ref EchoRayStep step)
    {
        // update our old position to be the previous new one
        step.OldPos = step.NewPos;

        // move forward by our translation
        step.NewPos += step.Translation;

        // calculate the distance between our updated points
        step.NewDistance = Vector2.Distance(step.OldPos, step.NewPos);
        step.RemainingDistance -= step.NewDistance;
        step.TotalDistance += step.NewDistance;
        // update our translation with the new distance
        step.Translation = step.Direction * step.NewDistance;
    }

    private void ProcessAudioEntity(Entity<AudioComponent> audioEnt)
    {
        TryProcessAreaSpaceMagnitude(_clientEnt, _maximumMagnitude, out var echoMagnitude);

        if (echoMagnitude > _minimumMagnitude)
        {
            var bestPreset = GetBestPreset(echoMagnitude);
            _audioEffectSystem.TryAddEffect(audioEnt, bestPreset);
        }
        else
            _audioEffectSystem.TryRemoveEffect(audioEnt);
    }

    // Maybe TODO: defer this onto ticks? but whatever its just clientside
    private void OnAudioParentChanged(Entity<AudioComponent> entity, ref EntParentChangedMessage args)
    {
        if (args.Transform.MapID == MapId.Nullspace)
            return;

        if (!CanAudioEcho(entity))
            return;

        if (!_playerManager.LocalEntity.HasValue)
            return;
        _clientEnt = _playerManager.LocalEntity.Value;

        ProcessAudioEntity(entity);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        _clientEnt = ev.Entity;
    }

    private struct EchoRayStep
    {
        public Vector2 OldPos;
        public Vector2 NewPos;
        public float NewDistance;
        public float TotalDistance;
        public float RemainingDistance;
        public float MaxProbeDistance;
        public Vector2 Direction;
        public Vector2 Translation;
        public Vector2 ProbeTranslation;
    }

    private struct EchoRayStats
    {
        public float TotalAbsorption;
        public int TotalBounces;
        public int TotalEscapes;
        public float Magnitude;
    }
}

/// <summary>
/// A class container containing thresholds for audio presets.
/// </summary>
public sealed class AudioDistanceThreshold(float distance, ProtoId<AudioPresetPrototype> preset)
{
    public float Distance { get; init; } = distance;
    public ProtoId<AudioPresetPrototype> Preset { get; init; } = preset;
}
