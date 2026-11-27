using Content.Shared.Radiation.Systems;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Create circle pulse animation of radiation around object.
///     Drawn on client after creation only once per component lifetime.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause] // imp, added AutoGenerateComponentPause]
[Access(typeof(SharedRadiationPulseSystem))] // imp, changed to SharedRadiationPulseSystem from RadiationPulseSystem
public sealed partial class RadiationPulseComponent : Component
{
    /// <summary>
    ///     Timestamp when component was assigned to this entity.
    /// </summary>
    public TimeSpan StartTime;

    /// <summary>
    ///     How long will animation play in seconds.
    ///     Can be overridden by <see cref="Robust.Shared.Spawners.TimedDespawnComponent"/>.
    /// </summary>
    public float VisualDuration = 2f;

    /// <summary>
    ///     The range of animation.
    ///     Can be overridden by <see cref="RadiationSourceComponent"/>.
    /// </summary>
    public float VisualRange = 5f;

    // imp start
    [ViewVariables(VVAccess.ReadOnly), AutoPausedField]
    public TimeSpan NextInRangeEvent;
    [DataField]
    public TimeSpan InRangeEventInterval = TimeSpan.FromSeconds(1f);
    // imp end
}
