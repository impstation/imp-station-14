using Content.Server._Impstation.StationEvents.Events;

namespace Content.Server._Impstation.StationEvents.Components;

/// <summary>
/// Used an event that discharges mass amounts of gas from the Supermatter
/// </summary>
[RegisterComponent, Access(typeof(SupermatterEventRule))]
public sealed partial class SupermatterEventRuleComponent : Component
{
    /// <summary>
    /// The entity uid of the supermatter selected
    /// </summary>
    [DataField]
    public EntityUid SupermatterUid;

    /// <summary>
    /// Locids of text that pops up when you're far too hot.
    /// </summary>
    [DataField]
    public List<LocId> SupermatterEvents = ["SupermatterDischarge", "SupermatterSurge"];

    /// <summary>
    /// Range that the EMP can disable devices in
    /// </summary>
    [DataField]
    public float EMPRange = 40f;

    /// <summary>
    /// Amount of energy that can be consumed from a power source, currently 100 MW
    /// </summary>
    [DataField]
    public float EmpEnergyConsumption = 100000000f;

    /// <summary>
    /// Duration of the EMP on a device
    /// </summary>
    [DataField]
    public TimeSpan EmpDisabledDuration = TimeSpan.FromSeconds(5);
}
