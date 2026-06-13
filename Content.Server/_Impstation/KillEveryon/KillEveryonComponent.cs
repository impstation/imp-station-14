namespace Content.Server._Impstation.KillEveryon;

/// <summary>
/// Gives the entity the kill everyon objective.
/// </summary>
[RegisterComponent]
public sealed partial class KillEveryonComponent : Component
{
    [DataField]
    public string Obj = "KillEveryonObjective";
}
