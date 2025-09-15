using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Impstation.Anomalocarid;

namespace Content.Server._Impstation.Anomalocarid;

public sealed partial class HeatVentSystem : SharedHeatVentSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeatVentComponent, HeatVentDoAfterEvent>(OnVent);
    }

    private void OnVent(Entity<HeatVentComponent> ent, ref HeatVentDoAfterEvent args)
    {
        var tileMix = _atmos.GetTileMixture(ent.Owner, excite: true);
        tileMix?.AdjustMoles(ent.Comp.VentGas, ent.Comp.HeatStored * ent.Comp.MolesPerHeatStored);
        // TODO: temperature?

        _popup.PopupEntity(ent.Comp.VentDoAfterPopup, ent);
        ent.Comp.HeatStored = 0;
    }
}
