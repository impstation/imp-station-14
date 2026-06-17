using Content.Server.Atmos.Piping.Components;
using Content.Shared.Wires;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegSystem
{
    [Dependency] private readonly GasOutletInjectorSystem _gasInjectorSystem = default!;

    /// <summary>
    /// Changes the state of the air injector given a bool.
    /// </summary>
    /// <param name="state">true for enabled, false for disabled</param>
    private void ChangeInjectorState(EntityUid uid, bool state)
    {
        if (!TryComp<GasOutletInjectorComponent>(uid, out var injector))
            return;

        _gasInjectorSystem.SetEnabled(uid, injector, state);
    }

    /// <returns>Whether this circulator is open.</returns>
    public bool IsOpen(EntityUid uid)
    {
        // Try to get the WiresPanel component and see if it's open.
        return TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open;
    }
}
