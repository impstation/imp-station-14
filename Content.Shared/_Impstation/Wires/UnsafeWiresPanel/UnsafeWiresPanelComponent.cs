namespace Content.Shared._Impstation.UnsafeWiresPanel;

[RegisterComponent]
[Access(typeof(UnsafeWiresPanelSystem))]
public sealed partial class UnsafeWiresPanelComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Delay = 2f;
}
