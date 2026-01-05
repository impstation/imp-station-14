using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Heretic.Components;

//for tracking the subject's trip through hell.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class InHellComponent : Component
{
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitHellTime = default!;

    [DataField]
    public EntityUid OriginalBody;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates OriginalPosition;

    [DataField]
    public EntityUid? Mind;

    [DataField]
    public Boolean HasMind = false;

    [DataField, AutoNetworkedField]
    public TimeSpan HellDuration = TimeSpan.FromSeconds(15);

    [DataField] public ProtoId<CloningSettingsPrototype> CloneSettings = "HellClone";
}
