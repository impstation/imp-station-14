namespace Content.Shared._Impstation.CPR;

[ByRefEvent]
public record struct PerformCprEvent(EntityUid Target, bool Cancelled = false);

[ByRefEvent]
public record struct ReceiveCprEvent(EntityUid Performer, bool Cancelled = false);
