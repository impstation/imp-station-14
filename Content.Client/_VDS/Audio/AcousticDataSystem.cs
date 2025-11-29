// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
// SPDX-FileCopyrightText: 2025 ark1368
// SPDX-FileCopyrightText: 2025 Jellvisk
//
// SPDX-License-Identifier: MPL-2.0

using Content.Client._Mono.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared._VDS.Physics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Content.Shared._VDS.Audio;
using Content.Shared._VDS.CCVars;

namespace Content.Client._VDS.Audio;
/// <summary>
/// Gathers environmental acoustic info around the player, later to be processed by <see cref="AudioEffectSystem"/>.
/// </summary>
public sealed class AcousticDataSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectSystem _audioEffectSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roofSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly ReflectiveRaycastSystem _reflectiveRaycast = default!;

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
        TODO: this could be expanded to be more than just these few presets. see ReverbPresets.cs in Robust.Shared/Audio/Effects/
        would require gathering more data.
    */
    private static readonly AudioDistanceThreshold[] DistancePresets =
    [
        new(4f, "SpaceStationCupboard"),
        new(6f, "DustyRoom"),
        new(12f, "SpaceStationSmallRoom"),
        new(15f, "SpaceStationShortPassage"),
        new(22f, "SpaceStationMediumRoom"),
        new(30f, "SpaceStationHall"),
        new(40f, "SpaceStationLargeRoom"),
        new(45f, "Auditorium"),
        new(47f, "ConcertHall"),
        new(60f, "Hangar")
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

    private int _acousticMaxReflections;
    private bool _acousticEnabled = true;

    private EntityQuery<AcousticDataComponent> _absorptionQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<RoofComponent> _roofQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(VCCVars.AcousticEnable, x => _acousticEnabled = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(VCCVars.AcousticReflectionCount, x => _acousticMaxReflections = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(VCCVars.AcousticHighResolution, x => _calculatedDirections = GetEffectiveDirections(x), invokeImmediately: true);

        _absorptionQuery = GetEntityQuery<AcousticDataComponent>();
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

    private void ProcessAcoustics(Entity<AudioComponent> audioEnt)
    {

        var magnitude = 0f;
        if (CastAndTryGetEnvironmentAcousticData(_clientEnt, _maximumMagnitude, _acousticMaxReflections, _calculatedDirections, out var acousticResults))
        {
            magnitude = CalculateAmplitude((_clientEnt, Transform(_clientEnt)), acousticResults);
        }

        if (magnitude > _minimumMagnitude)
        {
            var bestPreset = GetBestPreset(magnitude);
            _audioEffectSystem.TryAddEffect(audioEnt, bestPreset);
        }
        else
            _audioEffectSystem.TryRemoveEffect(audioEnt);
    }



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
    /// <param name="maxRange"> Total range a single ray can go before dying. </param>
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
    public bool CastAndTryGetEnvironmentAcousticData(
        in EntityUid originEnt,
        in float maxRange,
        in int maxBounces,
        in Angle[] castDirections,
        [NotNullWhen(true)] out List<AcousticRayResults>? acousticResults)
    {
        acousticResults = new List<AcousticRayResults>(castDirections.Length);

        if (!originEnt.IsValid()
            || !_transformQuery.HasComponent(originEnt))
            return false;

        // in space nobody can hear your awesome freaking acoustics
        if (!_turfSystem.TryGetTileRef(originEnt.ToCoordinates(), out var tileRef)
            || _turfSystem.IsSpace(tileRef.Value))
            return false;

        var clientTransform = Transform(originEnt);
        var clientMapId = clientTransform.MapID;
        var clientCoords = _transformSystem.ToMapCoordinates(clientTransform.Coordinates).Position;

        var pathFilter = new QueryFilter
        {
            MaskBits = (int)CollisionGroup.DoorPassable,
            IsIgnored = ent => !_absorptionQuery.HasComp(ent), // ideally we'd pass _absorptionQuery via state, but the new ray system doesn't allow that for some reason
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };
        var probeFilter = new QueryFilter
        {
            LayerBits = (int)CollisionGroup.HighImpassable,
            IsIgnored = ent => _absorptionQuery.TryGetComponent(ent, out var comp) && comp.ReflectRay == false,
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };

        var state = new ReflectiveRayState(
                probeFilter,
                pathFilter,
                origin: clientCoords,
                direction: Vector2.Zero, // we change the dir later
                maxRange: maxRange,
                clientMapId
                );

        // cast our rays and get our results
        acousticResults = CastManyReflectiveAcousticRays(
            originEnt,
            clientCoords,
            maxBounces,
            castDirections,
            ref state);

        if (acousticResults.Count == 0)
            return false;

        return true;
    }

    private List<AcousticRayResults> CastManyReflectiveAcousticRays(
            in EntityUid originEnt,
            in Vector2 originCoords,
            in int maxBounces,
            in Angle[] castDirections,
            ref ReflectiveRayState state)
    {
        var acousticResults = new List<AcousticRayResults>();

        foreach (var direction in castDirections)
        {
            var rand = _random.NextFloat(-1f, 1f);
            var offsetDirection = direction + rand;
            state.CurrentPos = originCoords;
            state.OldPos = originCoords;
            state.Direction = offsetDirection.ToVec();
            state.Translation = state.Direction * state.MaxRange;
            state.ProbeTranslation = state.Translation;
            state.RemainingDistance = state.MaxRange;


            // handle individual bounces
            var results = CastReflectiveAcousticRay(originEnt, maxBounces, ref state);
            acousticResults.Add(results);
        }

        return acousticResults;
    }

    private AcousticRayResults CastReflectiveAcousticRay(in EntityUid originEnt, in int maxBounces, ref ReflectiveRayState state)
    {
        var results = new AcousticRayResults();
        for (var bounce = 0; bounce < maxBounces; bounce++)
        {
            /*
                our raycast state will constantly be fed by reference into the reflective raycast API,
                which updates the reference's positional data for us, including the handling of
                bounces with each iteration. we also get a new list of entities for each iteration so we
                can do component data gathering on them.
            */
            var (probeResult, pathResults) = _reflectiveRaycast.CastAndUpdateReflectiveRayStateRef(ref state);

            results.TotalRange += state.CurrentSegmentDistance;
            if (probeResult.Hit)
            {
                pathResults.Results.Add(probeResult.Results[0]); // we wanna include what we hit to our data too
                results.TotalBounces++;
            }

            // gather acoustic component data
            if (pathResults.Results.Count > 0)
            {
                foreach (var result in pathResults.Results)
                {
                    if (!_absorptionQuery.TryGetComponent(result.Entity, out var comp))
                        continue;

                    // TODO: more component data can be gathered here in the future
                    results.TotalAbsorption += GetAcousticAbsorption(result, originEnt, state.CurrentSegmentDistance, comp);
                    // Logger.Debug($"got somethin: {ToPrettyString(result.Entity)}. new absorb: {results.TotalAbsorption}");
                }
            }

            // this ray is long enough to be considered in an open area and now shall be ignored
            if (state.CurrentSegmentDistance >= state.MaxRange * 0.5f)
            {
                results.TotalEscapes++;
                break;
            }

            // expended our range budget, break the loop
            if (results.TotalRange >= state.MaxRange)
                break;
        }
        return results;
    }

    private float GetAcousticAbsorption(
            in RayHit result,
            in EntityUid originEnt,
            in float segmentLength,
            in AcousticDataComponent comp)
    {
        // linear decay based on distance from the listener and the final ray distance.
        result.Entity.ToCoordinates().TryDistance(
                EntityManager,
                originEnt.ToCoordinates(),
                out var distance
                );
        var distanceFactor = NormalizeToPercentage(distance, 0f, segmentLength);
        // Logger.Debug($"distance factor = {distanceFactor}");
        return comp.Absorption * distanceFactor;
    }

    /// <summary>
    /// Basic check for whether an audio entity can be applied effects such as reverb.
    /// </summary>
    public bool CanAudioBePostProcessed(in Entity<AudioComponent> audio)
    {
        if (!_acousticEnabled)
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

    /// <summary>
    /// Calculates our the overall amplitude of <paramref name="acousticResults"/>.
    /// </summary>
    /// <param name="originEnt"> Where the rays originally came from, for roof detecting purposes. </param>
    /// <returns> Our amplitude. </returns>
    private float CalculateAmplitude(
        in Entity<TransformComponent> originEnt,
        in List<AcousticRayResults> acousticResults)
    {
        var totalRays = acousticResults.Count;
        var avgMagnitude = acousticResults.Average(mag => mag.TotalRange);
        var avgAbsorption = acousticResults.Average(absorb => absorb.TotalAbsorption);
        var escaped = acousticResults.Sum(escapees => escapees.TotalEscapes);
        // TODO: resonance??
        // var avgBounces = (float)acousticResults.Average(bounce => bounce.TotalBounces);

        if (_prevAvgMagnitude > float.Epsilon)
            avgMagnitude = MathHelper.Lerp(_prevAvgMagnitude, avgMagnitude, 0.25f);
        _prevAvgMagnitude = avgMagnitude;

        var amplitude = 0f;
        amplitude += avgMagnitude;
        amplitude *= InverseNormalizeToPercentage(avgAbsorption); // things like furniture or different material walls should eat our energy
        amplitude *= MathF.Max(InverseNormalizeToPercentage(escaped, 0f, totalRays), 0.15f); // if a ray is considered escaped, that audio aint comin' back or would die off anyway.

        // severely punish our amplitude if there is no roof.
        if (originEnt.Comp.GridUid.HasValue
            && _roofQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var roof)
            && _gridQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var grid)
            && _transformSystem.TryGetGridTilePosition(originEnt.Owner, out var indices)
            && !_roofSystem.IsRooved((originEnt.Comp.GridUid.Value, grid, roof), indices))
        {
            amplitude *= 0.3f;
        }

        // Logger.Debug($"""
        //         Acoustics:
        //         - Average Magnitude: {avgMagnitude:F2}
        //         - Average Absorption: {avgAbsorption:F2}
        //         - Average Escaped: {escaped:F2}
        //         - Rays: {totalRays}
        //
        //         - Absorb Coefficient: {InverseNormalizeToPercentage(avgAbsorption):F2}
        //         - Escape Coefficient: {MathF.Max(InverseNormalizeToPercentage(escaped, 0f, totalRays), 0.15f):F2}
        //         - Final Magnitude: {amplitude}
        //         - Preset: {GetBestPreset(amplitude)}
        //         """);

        return amplitude;
    }

    /// <summary>
    /// Returns an epsilon..1f percent, where the closer to 0 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    /// <param name="total">
    /// What to compare our value to. Defaults to 100f.
    /// </param>
    /// <returns>
    /// A multiplication friendly eps~1f value.
    /// </returns>
    public static float NormalizeToPercentage(float value, float minValue = 0f, float maxValue = 100f)
    {
        var percentage = (value - minValue) / (maxValue - minValue);
        return MathHelper.Clamp01(percentage);
    }

    /// <summary>
    /// Returns an epsilon..1f percent, where the closer to 0 the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    /// <param name="total">
    /// What to compare our value to. Defaults to 100f.
    /// </param>
    /// <returns>
    /// A multiplication friendly eps~1f value.
    /// </returns>
    public static float InverseNormalizeToPercentage(float value, float minValue = 0f, float maxValue = 100f)
    {
        var percentage = NormalizeToPercentage(value, minValue, maxValue);
        return 1f - percentage;
    }

    /// <summary>
    /// Data about the current acoustic environment and relevant variables.
    /// </summary>
    public struct AcousticRayResults
    {
        public float TotalAbsorption;
        public int TotalBounces;
        public int TotalEscapes;
        public float TotalRange;
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
