using Content.Shared.Whitelist;

namespace Content.Server._Impstation.Borgs.LawSync;

/// <summary>
/// Component for law synchronizer tools. When this item is used to interact with a compatible silicon,
/// it will add LawSyncedComponent to it.
/// </summary>
[RegisterComponent]
public sealed partial class LawSynchronizerComponent : Component
{
    /// <summary>
    /// Entities with components on this list will be synchronized, even if they have blacklisted components.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = null;
    /// <summary>
    /// Entities with components on this list will not be synchronized.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist = null;

    [DataField]
    public LocId SyncFailedIncompatiblePopup = "lawsync-synchronize-failed-incompatible";
    [DataField]
    public LocId SyncFailedWirePanelPopup = "lawsync-synchronize-failed-panel-not-open";
    [DataField]
    public LocId SyncSuccessfulPopup = "lawsync-synchronize-success";
    [DataField]
    public LocId DeSyncSuccessfulPopup = "lawsync-desynchronize-success";
}
