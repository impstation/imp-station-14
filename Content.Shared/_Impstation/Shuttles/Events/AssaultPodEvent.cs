using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Shuttles.Events;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AssaultPodPurchasedEvent : EntityEventArgs;

[ByRefEvent]
public record struct AssaultPodStartedEvent(EntityCoordinates Coords);
