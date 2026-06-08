using Content.Server.Atmos.Piping.Components;
using Content.Shared.Wires;
using Content.Server.Atmos.Piping.Unary.EntitySystems;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly GasPassiveVentSystem _passiveVentSystem = default!;

    /// <summary>
    /// Forwards the <see cref="AtmosDeviceUpdateEvent"> to the circulator if its panel is open.
    /// Causes the passive vent connected to the inlet to spill out.
    /// </summary>
    /// <returns>True if the circulator's WirePanel is open. False otherwise.</returns>
    private bool UpdateOpenCirculator(EntityUid uid, ref AtmosDeviceUpdateEvent args)
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
