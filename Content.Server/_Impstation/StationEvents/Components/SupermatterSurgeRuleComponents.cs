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
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid SupermatterUid;

    /// <summary>
    /// Minimum & maximum power that the supermatter can surge to
    /// </summary>
    [DataField]
    public MinMax Power = new(5000, 10000);

    /// <summary>
    /// Minimum & maximum heat modifier that the supermatter can surge to
    /// </summary>
    [DataField]
    public MinMax HeatModifier = new(1, 2);

    /// <summary>
    /// Time tracker for next explosive lightning strike
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextLightningTime;

    /// <summary>
    /// Minimum & maximum time until next explosive lightning strike
    /// </summary>
    [DataField]
    public MinMax LightningCooldown = new(15, 20);

    /// <summary>
    /// Range that the explosive lightning can strike in
    /// </summary>
    [DataField]
    public float ZapRange = 7f;

    /// <summary>
    /// Amount of explosive lightning strikes
    /// </summary>
    [DataField]
    public int ZapCount = 1;
}
