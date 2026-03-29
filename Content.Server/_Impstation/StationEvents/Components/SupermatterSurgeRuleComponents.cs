using Content.Server._Impstation.StationEvents.Events;

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
    /// Time tracker for when to begin supermatter surge
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilSurgeStart = 30f;

    /// <summary>
    /// Minimum power that the supermatter can surge to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinPowerSurge = 5000f;

    /// <summary>
    /// Maximum power that the supermatter can surge to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxPowerSurge = 10000f;

    /// <summary>
    /// Minimum heat modifier that the supermatter can surge to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinHeatSurge = 1f;

    /// <summary>
    /// Maximum heat modifier that the supermatter can surge to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxHeatSurge = 2f;

    /// <summary>
    /// Time tracker for next explosive lightning strike
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextLightning = 5f;

    /// <summary>
    /// Minimum time until next explosive lightning strike
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinTimeForLightning = 10f;

    /// <summary>
    /// Maximum time until next explosive lightning strike
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxTimeForLightning = 20f;

    /// <summary>
    /// Range that the explosive lightning can strike in
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ZapRange = 7f;

    /// <summary>
    /// Amount of explosive lightning strikes
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ZapCount = 2;
}
