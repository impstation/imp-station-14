using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Ghost;

/// <summary>
/// Component for the 'medium' reagent status effect. Lets affected entities see ghosts.
/// </summary>
[RegisterComponent]
public sealed partial class MediumComponent : Component
{
    /// <summary>
    /// Uses the same action as ghosts do
    /// </summary>
    [DataField]
    public EntProtoId ToggleGhostsMediumAction = "ActionToggleGhosts";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostsMediumActionEntity;
}
