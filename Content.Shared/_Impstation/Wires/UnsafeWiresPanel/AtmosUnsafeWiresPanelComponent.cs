using Content.Shared.Wires;
namespace Content.Shared._Impstation.UnsafeWiresPanel;

[RegisterComponent]
[Access(typeof(UnsafeWiresPanelSystem))]
public sealed partial class AtmosUnsafeWiresPanelComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float AdditionalDelay = 2f;
}
