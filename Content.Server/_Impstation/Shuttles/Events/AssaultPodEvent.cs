using Robust.Shared.Map;

namespace Content.Server._Impstation.Shuttles.Events;

[DataDefinition]
public sealed partial class AssaultPodPurchasedEvent : EntityEventArgs;

[ByRefEvent]
public record struct AssaultPodStartedEvent(EntityCoordinates Coords);
