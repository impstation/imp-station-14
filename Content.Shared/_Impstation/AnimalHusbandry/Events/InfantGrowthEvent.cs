namespace Content.Shared._Impstation.AnimalHusbandry.Events;

/// <summary>
/// Event that is raised whenever an animal progresses a growth stage.
/// </summary>
/// <param name="PreviousStage">The previous stage of growth</param>
/// <param name="NewStage">The new stage of growth</param>
/// <param name="Handled"></param>
[ByRefEvent]
public record struct InfantGrowthEvent(EntityUid PreviousStage, EntityUid NewStage, bool Handled = false);
