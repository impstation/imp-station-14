using Content.Shared._Impstation.Drone.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Drone
{
    public abstract class SharedDroneSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        [Serializable, NetSerializable]
        public enum DroneVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum DroneStatus : byte
        {
            Off,
            On
        }

        public override void Update(float frameTime)
        {
            var curTime = _timing.CurTime;
            var query = EntityQueryEnumerator<DroneComponent>();
            while (query.MoveNext(out var uid, out var drone))
            {
                if (curTime < drone.NextBatteryUpdate)
                    continue;

                drone.NextBatteryUpdate = curTime + TimeSpan.FromSeconds(1);
                Dirty(uid, drone);
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class DroneBuiState : BoundUserInterfaceState
    {
        public float ChargePercent;

        public bool HasBattery;

        public DroneBuiState(float chargePercent, bool hasBattery)
        {
            ChargePercent = chargePercent;
            HasBattery = hasBattery;
        }
    }

    [Serializable, NetSerializable]
    public enum DroneUiKey : byte
    {
        Key
    }
}
