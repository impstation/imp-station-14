namespace Content.Shared._Impstation.AnimalHusbandry.Events;

/// <summary>
/// Event that is raised whenever an animal is born.
/// </summary>
/// <param name="Infant"></param>
/// <param name="Handled"></param>
[ByRefEvent]
public record struct InfantBornEvent(EntityUid Infant, bool Handled = false);
