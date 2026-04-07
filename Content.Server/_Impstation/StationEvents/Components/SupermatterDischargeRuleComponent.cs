using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server._Impstation.StationEvents.Components;

/// <summary>
/// Used an event that discharges mass amounts of gas from the Supermatter
/// </summary>
[RegisterComponent, Access(typeof(SupermatterDischargeRule))]
public sealed partial class SupermatterDischargeRuleComponent : Component
{
    /// <summary>
    /// The entity uid of the supermatter selected
    /// </summary>
    [DataField]
    public EntityUid SupermatterUid;

    /// <summary>
    /// Original gas efficiency value of the target supermatter
    /// </summary>
    [DataField]
    public float BaseGasEfficiency;

    /// <summary>
    /// Gas efficiency during the supermatter discharge event
    /// </summary>
    [DataField]
    public float DischargeGasEfficiency = 40f;

    /// <summary>
    /// Time tracker for the next EMP discharge
    /// </summary>
    [DataField]
    public TimeSpan NextEMPTime;

    /// <summary>
    /// Minimum & maximum time until next EMP discharge
    /// </summary>
    [DataField]
    public MinMax EMPCooldownMinMax = new(10, 20);

    /// <summary>
    /// Range that the EMP can disable devices in
    /// </summary>
    [DataField]
    public float EMPRange = 20f;

    /// <summary>
    /// Amount of energy that can be consumed from a power source
    /// </summary>
    [DataField]
    public float EmpEnergyConsumption = 10000f;

    /// <summary>
    /// Duration of the EMP on a device
    /// </summary>
    [DataField]
    public TimeSpan EmpDisabledDuration = TimeSpan.FromSeconds(5);
}
