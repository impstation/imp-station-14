using Content.Shared.Popups;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Wires;
using Content.Shared._Funkystation.Atmos;

namespace Content.Shared._Impstation.UnsafeWiresPanel;

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
        if (args.Cancelled)
            return;

        Log.Debug("uwp recieved AttemptChangePanelEvent");

        // Get node group to iterate through
        if (!TryComp<NodeContainerComponent>(ent, out var nodes))
            return;

        // Find if any pipe goes over the max pressure threshold
        foreach (var node in nodes.Nodes.Values)
        {
            if (node is not IPipeNode pipe)
                continue;

            // Find the pressure of pipe.
            // TODO: seemingly impossible in Content.Shared?
            float pressure = float.MaxValue;
            if (pressure > ent.Comp.PressureKPaThreshold)
            {
                args.AdditionalDelay += ent.Comp.AdditionalDelay;
                _popup.PopupClient(Loc.GetString(ent.Comp.PopupLocString), ent, args.User, PopupType.MediumCaution);
                return;
            }
        }
    }
}
