using Content.Shared.Wires;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    /// <summary>
    ///     Called when the WiresPanel component changes to open with the PanelChangedEvent.
    ///     When a panel is opened, <see cref="UpdateOpenCirculator"> will be processed every <see cref="AtmosDeviceUpdateEvent"> via <see cref="GeneratorUpdate">.
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
}
