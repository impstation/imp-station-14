namespace Content.Server._Impstation.Cosmiccult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class InVoidComponent : Component
{
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitVoidTime = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid OriginalBody;
}
