using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Cosmiccult.Components;

/// <summary>
/// Component for targets being cleansed of corruption.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CleanseCorruptionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan CleansingExpirDuration = TimeSpan.FromSeconds(120);

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan CleanseTime = default!;
}
