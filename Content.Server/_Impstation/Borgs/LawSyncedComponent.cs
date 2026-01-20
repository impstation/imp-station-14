namespace Content.Server._Impstation.Borgs.LawSync;

[RegisterComponent]
public sealed partial class LawSyncedComponent : Component
{
    [DataField]
    public string TargetSlotId = "circuit_holder";
}
