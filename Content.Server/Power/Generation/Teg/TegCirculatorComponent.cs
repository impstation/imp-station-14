using Content.Shared.Atmos;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// A "circulator" for the thermo-electric generator (TEG).
/// Circulators are used by the TEG to take in a side of either hot or cold gas.
/// </summary>
/// <seealso cref="TegSystem"/>
[RegisterComponent]
[Access(typeof(TegSystem))]
public sealed partial class TegCirculatorComponent : Component
{
    /// <summary>
    /// The difference between the inlet and outlet pressure at the start of the previous tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastPressureDelta")]
    public float LastPressureDelta;

    /// <summary>
    /// The amount of moles transferred by the circulator last tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastMolesTransferred")]
    public float LastMolesTransferred;

    /// <summary>
    /// Minimum pressure delta between inlet and outlet for which the circulator animation speed is "fast".
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("visualSpeedDelta")]
    public float VisualSpeedDelta = 5 * Atmospherics.OneAtmosphere;

    /// <summary>
    /// Light color of this circulator when it's running at "slow" speed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lightColorSlow")]
    public Color LightColorSlow = Color.FromHex("#FF3300");

    /// <summary>
    /// Light color of this circulator when it's running at "fast" speed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lightColorFast")]
    public Color LightColorFast = Color.FromHex("#AA00FF");

    // IMP ADD START
    /// <summary>
    /// The minimum running efficiency at which the circulator will not take damage.
    /// </summary>
    [DataField]
    public float MinimumNominalEfficiency = 0.1f;

    /// <summary>
    /// The minimum fill level before a warning visual is displayed.
    /// </summary>
    public float WarningFillLevel = 0.25f;

    /// <summary>
    /// The maximum possible damage incurred per tick when efficiency is at 0.
    /// </summary>
    [DataField]
    public float MaximumDamagePerTick = 1f;

    /// <summary>
    /// The maximum integrity value.
    /// <see cref="Integrity"/> can not be restored above this value by normal gameplay means.
    /// </summary>
    [DataField]
    public float MaxIntegrity = 100f;

    /// <summary>
    /// The current integrity value. Triggers the failure state upon reaching 0.
    /// </summary>
    [DataField]
    public float Integrity = 100f; // TODO: Have this set to MaxIntegrity and make it not a datafield. Can't reference other vars in this scope

    /// <summary>
    /// The minimum integrity the circulator can run at before the running visuals change.
    /// </summary>
    [DataField]
    public float MinimumNominalIntegrity = 50f;

    /// <summary>
    /// The minimum and maximum size of the circulator's explosion.
    /// </summary>
    [DataField]
    public (float, float) ExplosionRadiusRange = (4f, 8f);
    // IMP ADD END
}
