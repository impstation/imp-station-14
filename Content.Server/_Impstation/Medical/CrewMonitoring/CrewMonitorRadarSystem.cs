using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Medical.SuitSensors;
using Content.Server.Station.Systems;
using Content.Shared._Impstation.Medical.CrewMonitoring;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Map;

namespace Content.Server._Impstation.Medical.CrewMonitoring;

public sealed class CrewMonitorRadarSystem : EntitySystem
{
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SuitSensorSystem _sensors = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewMonitorRadarComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(Entity<CrewMonitorRadarComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(args.Sender, out var device))
            return;
        var owningStation = _station.GetOwningStation(ent);
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        //Retrieve active server address if the sensor isn't connected to a server
        if (ent.Comp.ConnectedServer == null && owningStation != null)
        {
            if (_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(owningStation.Value, out var address))
            {
                ent.Comp.ConnectedServer = address;
            }
        }

        // Clear the connected server if its address isn't on the network
        if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, ent.Comp.ConnectedServer))
        {
            ent.Comp.ConnectedServer = null;
            return;
        }

        _deviceNetworkSystem.QueuePacket(ent, ent.Comp.ConnectedServer, _sensors.SuitSensorToPacket(sensorStatus));
    }
}
