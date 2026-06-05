using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Wires;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Piping.EntitySystems;
using System.Diagnostics;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly GasPassiveVentSystem _passiveVentSystem = default!;

    /// <summary>
    ///     Called when the WiresPanel component changes to open with the PanelChangedEvent.
    ///     When a panel is opened, <see cref="CirculatorOpen"> will be processed every <see cref="AtmosDeviceUpdateEvent"> via <see cref="GeneratorUpdate">.
    /// </summary>
    // private void OnPanelChanged(EntityUid uid, TegCirculatorComponent comp, PanelChangedEvent args)
    // {
    //     //If opened, add passive vent
    //     if (args.Open)
    //     {
    //         var passiveVent = AddComp<GasPassiveVentComponent>(uid);
    //         passiveVent.InletName = NodeNameInlet;

    //         Log.Debug("Added passive vent component");
    //     }

    //     //If closed, remove passive vent
    //     else
    //     {
    //         RemComp<GasPassiveVentComponent>(uid);
    //         Log.Debug("Removed passive vent component");
    //     }
    // }

    /// <summary>
    /// Forwards the <see cref="AtmosDeviceUpdateEvent"> to the circulator if its panel is open.
    /// Causes the passive vent connected to the inlet to spill out.
    /// </summary>
    /// <returns>True if the circulator's WirePanel is open. False otherwise.</returns>
    private bool CirculatorOpen(EntityUid uid, ref AtmosDeviceUpdateEvent args)
    {
        // Expel inlet contents if open
        // Log.Debug($"{uid} atmos update");

        // Check if panel is open
        if (!(TryComp(uid, out WiresPanelComponent? panel) && panel.Open))
            return false;

        // Expel contents via passive vent component.
        // Forward the AtmosDeviceUpdateEvent to the circulator.
        // The passive vent component will receive and process the event.
        RaiseLocalEvent(uid, ref args);
        return true;

        // Expel contents via passive vent system logic
        // GasMixture? environment = _atmosphere.GetContainingMixture(uid, args.Grid, args.Map, true, true);
        // var (inlet, _) = GetPipes(uid);

        // if (environment != null)
        //     _passiveVentSystem.VentFromPipeNode(inlet, environment);
        // return true;
    }
}
