using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server._Impstation.StationEvents.Components;

/// <summary>
/// Used an event that increases the power of the Supermatter
/// </summary>
[RegisterComponent, Access(typeof(SupermatterSurgeRule))]
public sealed partial class SupermatterSurgeRuleComponent : Component
{
    /// <summary>
    /// The entity uid of the supermatter selected
    /// </summary>
    [DataField]
    public EntityUid SupermatterUid;

    /// <summary>
    /// Minimum & maximum power that the supermatter can surge to
    /// </summary>
    [DataField]
    public MinMax PowerMinMax = new(5000, 10000);

    /// <summary>
    /// Minimum & maximum heat modifier that the supermatter can surge to
    /// </summary>
    [DataField]
    public (float, float) HeatModifierMinMax = (1f, 2f);

    /// <summary>
    /// Time tracker for next explosive lightning strike
    /// </summary>
    [DataField]
    public TimeSpan NextLightningTime;

    /// <summary>
    /// Minimum & maximum time until next explosive lightning strike
    /// </summary>
    [DataField]
    public MinMax LightningCooldownMinMax = new(10, 20);

    /// <summary>
    /// Range that the explosive lightning can strike in
    /// </summary>
    [DataField]
    public float ZapRange = 7f;

    /// <summary>
    /// Amount of explosive lightning strikes
    /// </summary>
    [DataField]
    public int ZapCount = 2;
}
