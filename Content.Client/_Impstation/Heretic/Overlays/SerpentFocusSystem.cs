using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared._EE.Overlays.Switchable;
using Robust.Client.Graphics;
using Content.Shared.Heretic.Components;

namespace Content.Client._EE.Overlays.Switchable;

public sealed class SerpentFocusSystem : Client.Overlays.EquipmentHudSystem<SerpentFocusComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private SerpentFocusOverlay _serpentOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SerpentFocusComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _serpentOverlay = new SerpentFocusOverlay();
    }

    protected override void OnRefreshComponentHud(Entity<SerpentFocusComponent> ent, ref RefreshEquipmentHudEvent<SerpentFocusComponent> args)
    {
        base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void OnRefreshEquipmentHud(Entity<SerpentFocusComponent> ent, ref InventoryRelayedEvent<RefreshEquipmentHudEvent<SerpentFocusComponent>> args)
    {
        if (!ent.Comp.IsEquipment)
            return;

        base.OnRefreshEquipmentHud(ent, ref args);
    }

    private void OnToggle(Entity<SerpentFocusComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<SerpentFocusComponent> args)
    {
        base.UpdateInternal(args);
        SerpentFocusComponent? tvComp = null;
        foreach (var comp in args.Components)
        {
            if (!comp.IsActive)
                continue;

            if (tvComp == null)
                tvComp = comp;
        }

        UpdateSerpentOverlay(tvComp);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        UpdateSerpentOverlay(null);
    }

    private void UpdateSerpentOverlay(SerpentFocusComponent? comp)
    {
        _serpentOverlay.Comp = comp;

        switch (comp)
        {
            case not null when !_overlayMan.HasOverlay<SerpentFocusOverlay>():
                _overlayMan.AddOverlay(_serpentOverlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_serpentOverlay);
                break;
        }
    }
}
