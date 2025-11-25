// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
// SPDX-FileCopyrightText: 2025 ark1368
// SPDX-FileCopyrightText: 2025 Jellvisk
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Coordinates;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared._Mono.CCVar;
using Content.Shared._VDS.Audio;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client._Mono.Audio;

/// <summary>
/// Gathers environmental acoustic info around the player, later to be processed by <see cref="AudioEffectSystem"/>.
/// </summary>
public sealed class AreareverbSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectSystem _audioEffectSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roofSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;

    /// <summary>
    /// The directions that are raycasted to determine size for reverb.
    /// Used relative to the grid.
    /// </summary>
    private Angle[] _calculatedDirections = [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];

    /// <summary>
    /// Values for the minimum arbitrary size at which a certain audio preset
    /// is picked for sounds.
    /// </summary>
    /// <remarks>
    /// Keep in ascending order.
    /// </remarks>
    /*  - VDS
        TODO: after reworking how acoustic data is collected, this could be expanded
        to be more than just these few presets. see ReverbPresets.cs in Robust.Shared/Audio/Effects/
    */
    private static readonly AudioDistanceThreshold[] DistancePresets =
    [
        new(18f, "Hallway"),
        new(30f, "Auditorium"),
        new(45f, "ConcertHall"),
        new(50f, "Hangar")
    ];

    private readonly float _minimumMagnitude = DistancePresets[0].Distance;
    private readonly float _maximumMagnitude = DistancePresets[^1].Distance; // neat way to get the last result of an array

    /// <summary>
    /// Our previously recorded magnitude, for lerp purposes.
    /// </summary>
    private float _prevAvgMagnitude;

    /// <summary>
    /// The client's local entity.
    /// </summary>
    private EntityUid _clientEnt;

    private int _reverbMaxReflections;
    private bool _advancedAudioEnabled = true;

    private EntityQuery<AudioAbsorptionComponent> _absorptionQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<RoofComponent> _roofQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoReflectionCount, x => _reverbMaxReflections = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(MonoCVars.AreaEchoEnabled, x => _advancedAudioEnabled = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(MonoCVars.AreaEchoHighResolution, x => _calculatedDirections = GetEffectiveDirections(x), invokeImmediately: true);

        _absorptionQuery = GetEntityQuery<AudioAbsorptionComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
        SubscribeLocalEvent<AudioComponent, EntParentChangedMessage>(OnParentChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    #region Events

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        _clientEnt = ev.Entity;
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        _clientEnt = EntityUid.Invalid;
    }

    private void OnParentChange(Entity<AudioComponent> audio, ref EntParentChangedMessage ev)
    {
        if (!CanAudioBePostProcessed(audio))
            return;

        ProcessAcoustics(audio);
    }
    #endregion

    private void ProcessAcoustics(Entity<AudioComponent> audioEnt)
    {
        var dataFilter = new QueryFilter
        {
            MaskBits = (int)CollisionGroup.DoorPassable,
            IsIgnored = ent => !_absorptionQuery.HasComp(ent), // ideally we'd pass _absorptionQuery via state, but the new ray system doesn't allow that for some reason
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };
        var collideFilter = new QueryFilter
        {
            LayerBits = (int)CollisionGroup.WallLayer,
            IsIgnored = ent => _absorptionQuery.TryGetComponent(ent, out var comp) && comp.ReflectRay,
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };

        var magnitude = 0f;
        if (TryGetEnvironmentAcousticData(_clientEnt, _maximumMagnitude, _reverbMaxReflections, dataFilter, collideFilter, out var envResults))
        {
            magnitude = CalculateAmplitude((_clientEnt, Transform(_clientEnt)), envResults);
        }

        if (magnitude > _minimumMagnitude)
        {
            var bestPreset = GetBestPreset(magnitude);
            _audioEffectSystem.TryAddEffect(audioEnt, bestPreset);
        }
        else
            _audioEffectSystem.TryRemoveEffect(audioEnt);
    }

    #region Get

    /// <summary>
    /// Returns the appropiate DistantPreset, or the largest if somehow it can't be found.
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
    /// Returns all four cardinal directions when <paramref name="highResolution"/> is false.
    ///     Otherwise, returns all eight intercardinal and cardinal directions as listed in
    ///     <see cref="DirectionExtensions.AllDirections"/>.
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

    /// <summary>
    /// Spawns several bouncing raycasts, which grabs acoustic data according to <paramref name="dataFilter"/>.
    /// Will not accept space as a valid place for acoustics, won't even bother to spawn any rays.
    /// </summary>
    /// <param name="originEnt"> What we'll spawn the rays on. </param>
    /// <param name="maxRayRange"> Total range a single ray can go before dying. </param>
    /// <param name="maxBounces"> Boing. </param>
    /// <param name="dataFilter"> What entities the rays should care about and steal component data from. </param>
    /// <param name="collideFilter"> What entities we'll boing off of. </param>
    /// <param name="acousticResults"> Gathered information about our environment. See <see cref="AcousticRayResults"/> </param>
    /// <param name="probeRange"></param>
    /// <remarks>
    /// The idea is to start these rays on the player instead of at every active audio source. For reverbs, this works
    /// great and is much faster than spawning rays at every audio source. If we wanted to get super crazy, we could
    /// cast rays from audio sources as well and calculate better audio muffling that way.
    ///
    /// Heavily inspired by Vercidium's video, see https://www.youtube.com/watch?v=u6EuAUjq92k .
    /// </remarks>
    /// <returns> True if any data was found. </returns>
    public bool TryGetEnvironmentAcousticData(
        in EntityUid originEnt,
        in float maxRayRange,
        in int maxBounces,
        in QueryFilter dataFilter,
        in QueryFilter collideFilter,
        [NotNullWhen(true)] out List<AcousticRayResults>? acousticResults,
        float? probeRange = null)
    {
        acousticResults = new List<AcousticRayResults>(_calculatedDirections.Length);

        if (!originEnt.IsValid()
            || !_transformQuery.HasComponent(originEnt))
            return false;

        var clientTransform = Transform(originEnt);
        var clientMapId = clientTransform.MapID;
        var clientCoords = _transformSystem.ToMapCoordinates(clientTransform.Coordinates).Position;

        // in space nobody can hear your awesome freaking acoustics
        if (!_turfSystem.TryGetTileRef(originEnt.ToCoordinates(), out var tileRef)
            || _turfSystem.IsSpace(tileRef.Value))
            return false;

        probeRange ??= maxRayRange;

        foreach (var direction in _calculatedDirections)
        {
            var rand = _random.NextFloat(-1f, 1f);
            var offsetDirection = direction + rand;
            CastAudioRay(
                    collideFilter,
                    dataFilter,
                    clientMapId, clientCoords, offsetDirection.ToVec(),
                    maxRayRange, maxBounces, probeRange.Value,
                    out var stats);

            acousticResults.Add(stats);
        }

        if (acousticResults.Count == 0)
        {
            return false;
        }

        return true;
    }

    #endregion
    #region Can

    /// <summary>
    /// Basic check for whether an audio entity can be applied effects such as reverb.
    /// </summary>
    public bool CanAudioBePostProcessed(in Entity<AudioComponent> audio)
    {
        if (!_advancedAudioEnabled)
            return false;

        // we cast from the player, so they need a valid entity.
        if (!_clientEnt.IsValid())
            return false;

        if (TerminatingOrDeleted(audio))
            return false;

        //  we only care about loaded local audio. it would be kinda weird
        //  if stuff like nukie music reverbed
        if (!audio.Comp.Loaded
            || audio.Comp.Global
            || audio.Comp.State == AudioState.Stopped)
            return false;

        // get audio grid or world pos so we can calculate if we're in hearing distance
        Vector2 audioPos;
        if ((audio.Comp.Flags & AudioFlags.GridAudio) != 0x0)
        {
            audioPos = _mapSystem.GetGridPosition(Transform(audio.Owner).ParentUid);
        }
        else
        {
            audioPos = _transformSystem.GetWorldPosition(audio.Owner);
        }

        // check distance!
        var delta = audioPos - _clientEnt.ToCoordinates().Position;
        var distance = delta.Length();
        if (_audioSystem.GetAudioDistance(distance) > audio.Comp.MaxDistance)
            return false;

        return true;
    }

    #endregion


    #region Helpers

    /// <summary>
    /// Calculates our the overall amplitude of <paramref name="acousticResults"/>.
    /// </summary>
    /// <param name="originEnt"> Where the rays originally came from, for roof detecting purposes. </param>
    /// <returns> Our amplitude. </returns>
    private float CalculateAmplitude(
        in Entity<TransformComponent> originEnt,
        in List<AcousticRayResults> acousticResults)
    {
        var totalRays = _calculatedDirections.Length;
        var avgMagnitude = acousticResults.Average(mag => mag.Magnitude);
        var avgAbsorption = acousticResults.Average(absorb => absorb.TotalAbsorption);
        var avgEscaped = (float)acousticResults.Average(escapees => escapees.TotalEscapes);
        // TODO: resonance??
        // var avgBounces = (float)acousticResults.Average(bounce => bounce.TotalBounces);

        if (_prevAvgMagnitude > float.Epsilon)
            avgMagnitude = MathHelper.Lerp(_prevAvgMagnitude, avgMagnitude, 0.25f);
        _prevAvgMagnitude = avgMagnitude;

        var amplitude = 0f;
        amplitude += avgMagnitude;
        amplitude *= InverseNormalizeToPercentage(avgAbsorption); // things like furniture or different material walls should eat our energy
        amplitude *= InverseNormalizeToPercentage(avgEscaped); // if a ray is considered escaped, that audio aint comin' back or would die off anyway.

        // severely punish our amplitude if there is no roof.
        if (originEnt.Comp.GridUid.HasValue
            && _roofQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var roof)
            && _gridQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var grid)
            && _transformSystem.TryGetGridTilePosition(originEnt.Owner, out var indices)
            && !_roofSystem.IsRooved((originEnt.Comp.GridUid.Value, grid, roof), indices))
        {
            amplitude *= 0.3f;
        }

        Logger.Debug($"""
                Acoustics:
                - Average Magnitude: {avgMagnitude:F2}
                - Average Absorption: {avgAbsorption:F2}
                - Average Escaped: {avgEscaped:F2}

                - Absorb Coefficient: {InverseNormalizeToPercentage(avgAbsorption, 100f):F2}
                - Escape Coefficient: {InverseNormalizeToPercentage(avgEscaped, 100f):F2}
                - Final Magnitude: {amplitude}
                - Preset: {GetBestPreset(amplitude)}
                """);

        return amplitude;
    }

    /// <summary>
    /// Returns an epsilon..1f percent, where the closer to 0 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    /// <param name="value">
    /// Our value to convert into a percent.
    /// </param>
    /// <param name="total">
    /// What to compare our value to. Defaults to 100f.
    /// </param>
    /// <returns>
    /// A multiplication friendly eps~1f value.
    /// </returns>
    public static float InverseNormalizeToPercentage(float value, float total = 100f)
    {
        return MathF.Max((total - value) / total * 1f, 0.01f);
    }

    /// <summary>
    /// Returns an epsilon..1f percent, where the closer to 0 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    /// <param name="value">
    /// Our value to convert into a percent.
    /// </param>
    /// <param name="total">
    /// What to compare our value to. Defaults to 100f.
    /// </param>
    /// <returns>
    /// A multiplication friendly eps~1f value.
    /// </returns>
    public static float NormalizeToPercentage(float value, float total = 100f)
    {
        return MathF.Max(value / total * 1f, 0.01f);
    }

    #endregion

    private void CastAudioRay(
        QueryFilter stopAtFilter, QueryFilter filter, MapId mapId, Vector2 startPos,
        Vector2 direction, float maxDistance, int maxIterations, float maxProbeLength,
        out AcousticRayResults rayStats)
    {
        var currentDirection = Vector2.Normalize(direction);
        var translation = currentDirection * maxDistance;
        var probeTranslation = currentDirection * maxProbeLength;

        var stepData = new BounceRayStepData
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

        rayStats = new AcousticRayResults
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
            var debugRay = new CollisionRay(stepData.OldPos, stepData.Direction, (int)stopAtFilter.LayerBits);
            var debugResults = _physicsSystem.IntersectRay(mapId, debugRay, stepData.NewDistance);
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

    private void UpdateAcousticData(ref AcousticRayResults stats, in RayHit hit, in float maxDistance, in EntityUid listener)
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

    private static void UpdateProbeStep(ref BounceRayStepData step, in Vector2 worldHitPos)
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

    private static void UpdateProbeStepReflect(ref BounceRayStepData step, in Vector2 worldNormal)
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

    private static void UpdateStepForward(ref BounceRayStepData step)
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


    #region Structs

    /// <summary>
    /// Data relevant to the location and direction of our bouncing ray.
    /// Updated each bounce.
    /// </summary>
    private struct BounceRayStepData
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

    /// <summary>
    /// Data about the current acoustic environment and relevant variables.
    /// </summary>
    public struct AcousticRayResults
    {
        public float TotalAbsorption;
        public int TotalBounces;
        public int TotalEscapes;
        public float Magnitude;
    }

    #endregion
}

/// <summary>
/// A class container containing thresholds for audio presets.
/// </summary>
public sealed class AudioDistanceThreshold(float distance, ProtoId<AudioPresetPrototype> preset)
{
    public float Distance { get; init; } = distance;
    public ProtoId<AudioPresetPrototype> Preset { get; init; } = preset;
}
