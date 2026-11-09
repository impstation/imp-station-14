using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Radiation.Events;

[Serializable, NetSerializable]
public sealed class RadiationPulseInRangeEvent(NetEntity pulse, MapCoordinates coords, TimeSpan nextExpectedUpdate, TimeSpan startTime, float visualDuration, float visualRange) : EntityEventArgs
{
    public readonly NetEntity Pulse = pulse;
    public readonly MapCoordinates Coords = coords;
    public readonly TimeSpan NextExpectedUpdate = nextExpectedUpdate;
    public readonly TimeSpan StartTime = startTime;
    public readonly float VisualDuration = visualDuration;
    public readonly float VisualRange = visualRange;
}
