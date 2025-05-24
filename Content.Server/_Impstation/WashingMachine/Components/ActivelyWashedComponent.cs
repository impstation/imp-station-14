namespace Content.Server._Impstation.WashingMachine.Components;

/// <summary>
/// Attached to an object that's actively being washed
/// </summary>
[RegisterComponent]
public sealed partial class ActivelyWashedComponent : Component
{
    /// <summary>
    /// The washing machine this entity is actively being washed by.
    /// </summary>
    [DataField]
    public EntityUid? WashingMachine;
}
