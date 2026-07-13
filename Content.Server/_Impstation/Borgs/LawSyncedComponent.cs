namespace Content.Server._Impstation.Borgs.LawSync;

/// <summary>
/// Added to lawbound silicons to synchronize their LawSet with that of the station's AI upload console.
/// </summary>
[RegisterComponent]
public sealed partial class LawSyncedComponent : Component
{
    /// <summary>
    /// The ItemSlot ID of the target AI upload console's lawboard slot.
    /// </summary>
    [DataField]
    public string TargetSlotId = "circuit_holder";
}
