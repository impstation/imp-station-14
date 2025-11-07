using Content.Shared.Medical.SuitSensor;

namespace Content.Shared._Impstation.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitorRadarComponent : Component
{
    /// <summary>
    ///     CurrentlyConnectedServer
    /// </summary>
    [DataField]
    public string? ConnectedServerAddress;


    /// <summary>
    ///     List of all currently connected sensors to this radar.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;


}
