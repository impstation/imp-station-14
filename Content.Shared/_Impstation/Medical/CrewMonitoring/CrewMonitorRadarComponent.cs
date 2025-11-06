using Content.Shared.Medical.SuitSensor;

namespace Content.Shared._Impstation.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitorRadarComponent : Component
{
    /// <summary>
    ///     The server the rdar sends to.
    ///     The suit sensor will try connecting to a new server when no server is connected.
    ///     It does this by calling the servers entity system for performance reasons.
    /// </summary>
    [DataField("server")]
    public string? ConnectedServer = null;

    /// <summary>
    ///     List of all currently connected sensors to this radar.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
