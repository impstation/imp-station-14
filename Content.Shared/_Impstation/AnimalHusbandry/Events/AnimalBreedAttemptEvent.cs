using Content.Shared._Impstation.AnimalHusbandry.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Events;

/// <summary>
/// Raised just before an animal attempts to breed.
/// </summary>
/// <param name="Partner">The mob that is approaching this one</param>>
/// <param name="Cancelled">Whether to stop the breeding taking place</param>
[ByRefEvent]
public record struct AnimalBreedAttemptEvent(EntityUid Partner, bool Cancelled = false);

/// <summary>
/// Raised just after an animal has bred and had the offspring selected.
/// </summary>
/// <param name="Handled"></param>
[ByRefEvent]
public record struct AnimalBreedCompletionEvent(EntityUid Partner, bool Handled = false);
