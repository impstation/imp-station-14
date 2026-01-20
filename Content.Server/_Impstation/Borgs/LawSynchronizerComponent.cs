namespace Content.Server._Impstation.Borgs.LawSync;

[RegisterComponent]
public sealed partial class LawSynchronizerComponent : Component
{
    [DataField]
    public string TargetSlotId = "circuit_holder";

    [DataField]
    public LocId SyncFailedWirePanelPopup = "lawsync-synchronize-failed-panel-not-open";
    [DataField]
    public LocId SyncSuccessfulPopup = "lawsync-synchronize-success";
}
