using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Ghost;

[RegisterComponent]
public sealed partial class MediumComponent : Component
{
    [DataField]
    public EntProtoId ToggleGhostsMediumAction = "ActionToggleGhosts";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostsMediumActionEntity;
}
