namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Sent to each Slasher when the effigy locator should be added or removed.
/// </summary>
/// <param name="Effigy">The active effigy, null if the locator should be removed.</param>
[ByRefEvent]
public record struct SlasherEffigyLocatorChangedEvent(EntityUid? Effigy);