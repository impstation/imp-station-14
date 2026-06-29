using Content.Shared.Wires;
namespace Content.Shared._Impstation.UnsafeWiresPanel;

[RegisterComponent]
[Access(typeof(UnsafeWiresPanelSystem))]
public sealed partial class AtmosUnsafeWiresPanelComponent : UnsafeWiresPanelComponent
{
    public override float Condition(Entity<UnsafeWiresPanelComponent> ent, ref AttemptChangePanelEvent args)
    {
        return 1f;
    }
}
