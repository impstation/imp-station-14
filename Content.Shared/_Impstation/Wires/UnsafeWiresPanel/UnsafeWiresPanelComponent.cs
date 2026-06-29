using Content.Shared.Wires;
namespace Content.Shared._Impstation.UnsafeWiresPanel;

[RegisterComponent]
[Access(typeof(UnsafeWiresPanelSystem))]
public abstract partial class UnsafeWiresPanelComponent : Component
{
    [DataField]
    public bool Enabled = true;

    public abstract float Condition(Entity<UnsafeWiresPanelComponent> ent, ref AttemptChangePanelEvent args);
}
