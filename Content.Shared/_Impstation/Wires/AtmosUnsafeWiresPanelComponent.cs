namespace Content.Shared._Impstation.Wires;

[RegisterComponent]
[Access(typeof(SharedUnsafeWiresPanelSystem))]
public sealed partial class AtmosUnsafeWiresPanelComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float PressureKPaThreshold = 25f;

    [DataField]
    public float AdditionalDelay = 2f;

    [DataField]
    public string PopupLocString = "comp-atmos-unsafe-unanchor-warning";
}
