using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            // check if sensor is ready to update
            if (curTime < sensor.NextUpdate)
                continue;

            if (!CheckSensorAssignedStation((uid, sensor)))
                continue;

            sensor.NextUpdate += sensor.UpdateRate;

            // get sensor status
            var status = GetSensorState((uid, sensor));
            if (status == null)
                continue;

            // Send it to the connected server
            var payload = SuitSensorToPacket(status);

            _protoManager.Resolve(sensor.Frequency, out var frequency);
            if (frequency != null)
            {
                _deviceNetworkSystem.QueuePacket(uid, null, payload, frequency: frequency.Frequency, device: device);
            }
        }
    }
}
