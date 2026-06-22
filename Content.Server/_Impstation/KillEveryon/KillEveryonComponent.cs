namespace Content.Server._Impstation.KillEveryon;

/// <summary>
/// Gives the entity the kill everyon objective.
/// </summary>
[RegisterComponent]
public sealed partial class KillEveryonComponent : Component
{
    /// <summary>
    /// The objective to be given to the shrimp
    /// </summary>
    [DataField]
    public string Obj = "KillEveryonObjective";
}
