using Content.Shared.Humanoid.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class HellVictimComponent : Component
{
    [DataField]
    public bool AlreadyHelled = false;

    [DataField]
    public bool CleanupDone = false;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitHellTime = default!;

    [DataField]
    public EntityUid OriginalBody;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates OriginalPosition;

    [DataField]
    public SpeciesPrototype? CloneProto;

    [DataField]
    public EntityUid? Mind;

    [DataField]
    public Boolean HasMind = false;

    [DataField, AutoNetworkedField]
    public TimeSpan HellDuration = TimeSpan.FromSeconds(15);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "SacrificedFaction";

}
