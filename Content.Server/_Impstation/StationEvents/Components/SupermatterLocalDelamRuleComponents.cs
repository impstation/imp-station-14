using Content.Server._Impstation.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StationEvents.Components;

/// <summary>
/// Used an event that discharges mass amounts of gas from the Supermatter
/// </summary>
[RegisterComponent, Access(typeof(SupermatterLocalDelamRule))]
public sealed partial class SupermatterLocalDelamRuleComponent : Component
{
    /// <summary>
    /// The entity uid of the supermatter selected
    /// </summary>
    [DataField]
    public EntityUid SupermatterUid;

    /// <summary>
    /// Events that can be started if there is a supermatter
    /// </summary>
    [DataField]
    public List<EntProtoId> SupermatterEvents = ["SupermatterDischarge", "SupermatterSurge"];

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
