using Content.Shared.Wires;

namespace Content.Shared._Impstation.UnsafeWiresPanel;

public sealed class UnsafeWiresPanelSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnsafeWiresPanelComponent, AttemptChangePanelEvent>(OnAttemptPanelChange);
    }

    private void OnAttemptPanelChange(Entity<UnsafeWiresPanelComponent> ent, ref AttemptChangePanelEvent args)
    {
        Log.Debug("uwp recieved AttemptChangePanelEvent");
    }
}
