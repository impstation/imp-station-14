using Content.Server.NodeContainer.Nodes;
using Content.Shared.Popups;
using Content.Shared.NodeContainer;
using Content.Shared.Wires;


namespace Content.Server._Impstation.Wires;

public sealed class UnsafeWiresPanelSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosUnsafeWiresPanelComponent, AttemptChangePanelEvent>(OnAttemptPanelChangeAtmos);
    }

    private void OnAttemptPanelChangeAtmos(Entity<AtmosUnsafeWiresPanelComponent> ent, ref AttemptChangePanelEvent args)
    {
        Log.Debug("uwp recieved AttemptChangePanelEvent");
        // args.AdditionalDelay += ent.Comp.AdditionalDelay;
        // _popup.PopupClient(Loc.GetString(ent.Comp.PopupLocString), ent, args.User, PopupType.MediumCaution);
        // return;

        if (args.Cancelled)
            return;

        // Get node group to iterate through
        if (!TryComp<NodeContainerComponent>(ent, out var nodes))
            return;

        // Find if any pipe goes over the max pressure threshold
        foreach (var node in nodes.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            // Find the pressure of pipe.
            float pressure = pipe.Air.Pressure;
            if (pressure > ent.Comp.PressureKPaThreshold)
            {
                args.AdditionalDelay += ent.Comp.AdditionalDelay;
                _popup.PopupEntity(Loc.GetString(ent.Comp.PopupLocString), ent, PopupType.MediumCaution);
                return;
            }
        }
    }

}
