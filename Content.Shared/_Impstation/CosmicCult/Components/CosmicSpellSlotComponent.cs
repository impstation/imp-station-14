using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Cosmiccult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicSpellSlotComponent : Component
{

    #region Actions

    [DataField]
    public EntProtoId CosmicToolAction = "ActionCosmicToolToggle";

    [DataField]
    public EntityUid? CosmicToolActionEntity;

    [DataField]
    public EntProtoId CosmicSiphonAction = "ActionCosmicSiphon";

    [DataField]
    public EntityUid? CosmicSiphonActionEntity;

    [DataField]
    public EntProtoId CosmicBlankAction = "ActionCosmicBlank";

    [DataField]
    public EntityUid? CosmicBlankActionEntity;

    [DataField]
    public EntProtoId CosmicLapseAction = "ActionCosmicLapse";

    [DataField]
    public EntityUid? CosmicLapseActionEntity;

    [DataField]
    public EntProtoId CosmicMonumentAction = "ActionCosmicPlaceMonument";

    [DataField]
    public EntityUid? CosmicMonumentActionEntity;

    #endregion

}
