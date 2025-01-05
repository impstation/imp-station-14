namespace Content.Server._Impstation.Cosmiccult.Components;

[RegisterComponent]
public sealed partial class InVoidComponent : Component
{
    /// <summary>
    ///     Length of the cooldown in between tile corruptions.
    /// </summary>
    [DataField]
    public float VoidTimeoutDuration = 22;

    /// <summary>
    ///     Counter for the timer.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float VoidTimeTicker = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid OriginalBody;
}
