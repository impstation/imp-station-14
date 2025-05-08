namespace Content.Shared._Impstation.CPR;

[RegisterComponent]
public sealed partial class CprGiverComponent : Component
{
    /// <summary>
    /// The amount that the target's UpdateInterval from their RespiratorComponent
    /// is multiplied by to determine the length of the CPR do-after.
    /// </summary>
    [DataField]
    public float IntervalMultiplier = 2f;

    /// <summary>
    /// The maximum potential length of a CPR do-after.
    /// </summary>
    [DataField]
    public float IntervalMax = 6f;
}
