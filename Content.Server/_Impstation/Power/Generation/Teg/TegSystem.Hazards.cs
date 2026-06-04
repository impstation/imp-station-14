using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Wires;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{

    private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    {
        //If opened, add passive vent
        if (args.Open)
        {
            // Solution 1: Add a passive vent component
            GasPassiveVentComponent passiveVent = AddComp<GasPassiveVentComponent>(uid);
            passiveVent.InletName = "inlet";
            Log.Debug("Added passive vent component");
        }

        //If closed, remove passive vent
        else
        {
            RemComp<GasPassiveVentComponent>(uid);
            Log.Debug("Removed passive vent component");
        }
    }
}
