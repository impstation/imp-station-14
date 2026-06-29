using Content.Shared.Wires;

namespace Content.Shared._Impstation.UnsafeWiresPanel;

public sealed class UnsafeWiresPanelSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosUnsafeWiresPanelComponent, AttemptChangePanelEvent>(OnAttemptPanelChangeAtmos);
    }

    private void OnAttemptPanelChangeAtmos(Entity<AtmosUnsafeWiresPanelComponent> ent, ref AttemptChangePanelEvent args)
    {
        Log.Debug("uwp recieved AttemptChangePanelEvent");
        args.AdditionalDelay += ent.Comp.AdditionalDelay;
    }
}
