namespace Content.Shared._Impstation.Homunculi.Incubator.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveIncubatorComponent : Component
{
    /// <summary>
    ///     When incubation is finished.
    /// </summary>
    [DataField, AutoPausedField, Access(typeof(SharedIncubatorSystem))]
    public TimeSpan? IncubationFinishTime;
}
