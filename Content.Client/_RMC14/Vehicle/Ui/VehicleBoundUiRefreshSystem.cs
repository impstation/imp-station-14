using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vehicle;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleBoundUiRefreshSystem : EntitySystem
{
    // [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsComponent, AfterAutoHandleStateEvent>(OnHardpointState);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, AfterAutoHandleStateEvent>(OnAmmoLoaderState);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, AfterAutoHandleStateEvent>(OnWeaponsSeatState);
        // SubscribeLocalEvent<VehicleSupplyConsoleComponent, AfterAutoHandleStateEvent>(OnSupplyConsoleState);
    }

    private void OnHardpointState(Entity<HardpointSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // _rmcUI.RefreshUIs<HardpointBoundUserInterface>(ent.Owner);
        RefreshUi<HardpointBoundUserInterface>(ent.Owner); // imp
    }

    private void OnAmmoLoaderState(Entity<VehicleAmmoLoaderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // _rmcUI.RefreshUIs<VehicleAmmoLoaderBoundUserInterface>(ent.Owner);
        RefreshUi<VehicleAmmoLoaderBoundUserInterface>(ent.Owner); // imp
    }

    private void OnWeaponsSeatState(Entity<VehicleWeaponsSeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // _rmcUI.RefreshUIs<VehicleWeaponsBoundUserInterface>(ent.Owner);
        RefreshUi<VehicleWeaponsBoundUserInterface>(ent.Owner); // imp
    }

    // private void OnSupplyConsoleState(Entity<VehicleSupplyConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    // {
    //     _rmcUI.RefreshUIs<VehicleSupplyBui>(ent.Owner);
    // }

    // IMP
    private void RefreshUi<T>(EntityUid ent) where T : BoundUserInterface, IRefreshableBui
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterface))
            return;

        foreach (var bui in userInterface.ClientOpenInterfaces)
        {
            if (bui is T ui)
                ui.Refresh();
        }
    }
}
