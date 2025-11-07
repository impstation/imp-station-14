using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Medical.SuitSensors;
using Content.Server.Station.Systems;
using Content.Shared._Impstation.Medical.CrewMonitoring;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Medical.CrewMonitoring;

public sealed class CrewMonitorRadarSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private const float UpdateRate = 3f; //im sorry im soorry but update cant get component variables w/o a trycomp and. yeah.
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewMonitorRadarComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(Entity<CrewMonitorRadarComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        ent.Comp.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;
        var query = EntityQueryEnumerator<CrewMonitorRadarComponent>();
        while (query.MoveNext(out var uid, out var radar))
        {
            UpdateTimeout((uid,radar));
            BroadcastSensorStatus((uid, radar));
        }
    }
    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(Entity<CrewMonitorRadarComponent> ent)
    {
        foreach (var (address, sensor) in ent.Comp.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > ent.Comp.SensorTimeout)
                ent.Comp.SensorStatus.Remove(address);
        }
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(Entity<CrewMonitorRadarComponent> ent)
    {
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = ent.Comp.SensorStatus,
        };

        //Retrieve active server address if the sensor isn't connected to a server

        var stationUid = _station.GetOwningStation(ent);

        if (ent.Comp.ConnectedServerAddress == null && stationUid != null)
        {
            if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(stationUid.Value, out var address))
                return;
            ent.Comp.ConnectedServerAddress = address;
        }

        _deviceNetworkSystem.QueuePacket(ent, ent.Comp.ConnectedServerAddress, payload);
    }
}
