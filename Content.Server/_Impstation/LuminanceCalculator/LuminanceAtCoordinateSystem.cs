using System;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Impstation.Lighting;

/// <summary>
/// Comparison operators supported by <see cref="LuminanceAtCoordinateSystem"/> threshold checks.
/// </summary>
public enum LuminanceComparison
{
    LessThan,
    LessThanOrEqual,
    Equal,
    GreaterThanOrEqual,
    GreaterThan,
}

/// <summary>
/// Measures how much unobstructed point-light luminance reaches a map coordinate from lights inside the server's PVS distance bounds.
/// Slasher abilities use this to decide whether a spot counts as dark enough without scanning lights far outside normal visibility range.
/// </summary>
public sealed class LuminanceAtCoordinateSystem : EntitySystem
{
    /// <summary>
    /// Tolerance used for float equality checks in <see cref="LuminanceComparison.Equal"/> mode.
    /// </summary>
    private const float EqualEpsilon = 0.0001f;

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private bool _clampToPvs;
    private float _standardPvsRange;
    private float _priorityPvsRange;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CVars.NetPVS, value => _clampToPvs = value, true);
        _cfg.OnValueChanged(CVars.NetMaxUpdateRange, value => RecalculatePvsRanges(value, null), true);
        _cfg.OnValueChanged(CVars.NetPvsPriorityRange, value => RecalculatePvsRanges(null, value), true);
    }

    private void RecalculatePvsRanges(float? standardPvsRange, float? priorityPvsRange)
    {
        if (standardPvsRange.HasValue)
            _standardPvsRange = standardPvsRange.Value / 2f;

        if (priorityPvsRange.HasValue)
            _priorityPvsRange = priorityPvsRange.Value / 2f;

        _priorityPvsRange = Math.Max(_standardPvsRange, _priorityPvsRange);
    }

    /// <summary>
    /// Measures local point-light luminance at a map coordinate, clamped to the same distance limits the server uses for regular and priority PVS updates.
    /// </summary>
    /// <param name="mapCoords">Coordinate to sample.</param>
    /// <param name="ambientThreshold">Maximum total luminance that still counts as dark enough.</param>
    /// <param name="comparison">Comparison operator used against the threshold. Defaults to less-than-or-equal for existing behavior.</param>
    public LuminanceCheckResult Evaluate(MapCoordinates mapCoords,
        float ambientThreshold,
        LuminanceComparison comparison = LuminanceComparison.LessThanOrEqual)
    {
        var pointLuminance = 0f;

        var lights = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (lights.MoveNext(out _, out var light, out var lightXform))
        {
            TryAccumulateLightContribution(mapCoords,
                light,
                lightXform,
                _clampToPvs,
                _standardPvsRange,
                _priorityPvsRange,
                ref pointLuminance);
        }

        return new LuminanceCheckResult(CompareLuminance(pointLuminance, ambientThreshold, comparison),
            pointLuminance,
            ambientThreshold);
    }

    /// <summary>
    /// Compares measured luminance against the configured threshold using the selected operator.
    /// </summary>
    /// <param name="pointLuminance">Measured luminance at the sampled coordinate.</param>
    /// <param name="ambientThreshold">Threshold value to compare against.</param>
    /// <param name="comparison">Comparison operator to apply.</param>
    /// <returns>True when the selected comparison is satisfied.</returns>
    private static bool CompareLuminance(float pointLuminance,
        float ambientThreshold,
        LuminanceComparison comparison)
    {
        return comparison switch
        {
            LuminanceComparison.LessThan => pointLuminance < ambientThreshold,
            LuminanceComparison.LessThanOrEqual => pointLuminance <= ambientThreshold,
            LuminanceComparison.Equal => MathF.Abs(pointLuminance - ambientThreshold) <= EqualEpsilon,
            LuminanceComparison.GreaterThanOrEqual => pointLuminance >= ambientThreshold,
            LuminanceComparison.GreaterThan => pointLuminance > ambientThreshold,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unhandled luminance comparison value."),
        };
    }

    /// <summary>
    /// Adds one light's contribution to the total luminance when it is eligible by map, distance, PVS bounds, and occlusion.
    /// </summary>
    /// <param name="mapCoords">Coordinate being sampled.</param>
    /// <param name="light">Light component being evaluated.</param>
    /// <param name="lightXform">Transform component for the light entity.</param>
    /// <param name="clampToPvs">Whether PVS distance clamping is active.</param>
    /// <param name="standardPvsRange">Standard PVS half-range used for non-priority lights.</param>
    /// <param name="priorityPvsRange">Priority PVS half-range used for priority lights.</param>
    /// <param name="pointLuminance">Running luminance sum to update.</param>
    private void TryAccumulateLightContribution(MapCoordinates mapCoords,
        PointLightComponent light,
        TransformComponent lightXform,
        bool clampToPvs,
        float standardPvsRange,
        float priorityPvsRange,
        ref float pointLuminance)
    {
        if (!light.Enabled || light.Energy <= 0f || lightXform.MapID != mapCoords.MapId)
            return;

        var lightPos = _xform.GetWorldPosition(lightXform);
        var delta = lightPos - mapCoords.Position;
        var dist = delta.Length();
        if (light.Radius <= 0f || dist >= light.Radius)
            return;

        if (!IsLightInPvsRange(delta, light, clampToPvs, standardPvsRange, priorityPvsRange))
            return;

        var lightMapCoords = new MapCoordinates(lightPos, mapCoords.MapId);
        if (!_examine.InRangeUnOccluded(mapCoords, lightMapCoords, dist + 0.01f, null))
            return;

        var normalizedDist = dist / light.Radius;
        var attenuation = 1f - normalizedDist;
        if (attenuation <= 0f)
            return;

        var lightLuminance = (light.Color.R * 0.2126f)
            + (light.Color.G * 0.7152f)
            + (light.Color.B * 0.0722f);
        pointLuminance += lightLuminance * light.Energy * attenuation;
    }

    /// <summary>
    /// Checks whether a light is inside the server's square PVS bounds for the sampled coordinate.
    /// </summary>
    /// <param name="delta">Vector from sampled coordinate to light position.</param>
    /// <param name="light">Light component being evaluated.</param>
    /// <param name="clampToPvs">Whether PVS distance clamping is active.</param>
    /// <param name="standardPvsRange">Standard PVS half-range used for non-priority lights.</param>
    /// <param name="priorityPvsRange">Priority PVS half-range used for priority lights.</param>
    /// <returns>True when the light is in range or clamping is disabled.</returns>
    private static bool IsLightInPvsRange(System.Numerics.Vector2 delta,
        PointLightComponent light,
        bool clampToPvs,
        float standardPvsRange,
        float priorityPvsRange)
    {
        if (!clampToPvs)
            return true;

        var pvsRange = IsHighPriorityLight(light)
            ? priorityPvsRange
            : standardPvsRange;

        return MathF.Abs(delta.X) <= pvsRange
            && MathF.Abs(delta.Y) <= pvsRange;
    }

    /// <summary>
    /// Determines whether a light should use the server's priority PVS range bucket.
    /// </summary>
    /// <param name="light">Light component being evaluated.</param>
    /// <returns>True when the light meets the server's priority-light predicate.</returns>
    private static bool IsHighPriorityLight(PointLightComponent light)
    {
        return light is { Enabled: true, CastShadows: true, Radius: > 7, LifeStage: <= ComponentLifeStage.Running };
    }
}

/// <summary>
/// Result of checking whether a coordinate is at a specific luminance level, including the actual measured point luminance and the threshold for reference.
/// </summary>
/// <param name="MeetsThreshold">Whether the measured luminance satisfies the selected comparison.</param>
/// <param name="PointLuminance">Measured luminance at the sampled coordinate.</param>
/// <param name="AmbientThreshold">Configured threshold used for comparison.</param>
public readonly record struct LuminanceCheckResult(
    bool MeetsThreshold,
    float PointLuminance,
    float AmbientThreshold);
