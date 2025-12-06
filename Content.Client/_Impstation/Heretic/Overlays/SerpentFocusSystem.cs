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
    private BaseSwitchableOverlay<SerpentFocusComponent> _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SerpentFocusComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _serpentOverlay = new SerpentFocusOverlay();
        _overlay = new BaseSwitchableOverlay<SerpentFocusComponent>();
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
        var lightRadius = 0f;
        foreach (var comp in args.Components)
        {
            if (!comp.IsActive)
                continue;

            if (tvComp == null)
                tvComp = comp;

            lightRadius = MathF.Max(lightRadius, comp.LightRadius);
        }

        _serpentOverlay.ResetLight(false);
        UpdateSerpentOverlay(tvComp, lightRadius);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        UpdateSerpentOverlay(null, 0f);
    }

    private void UpdateSerpentOverlay(SerpentFocusComponent? comp, float lightRadius)
    {
        _serpentOverlay.LightRadius = lightRadius;
        _serpentOverlay.Comp = comp;

        switch (comp)
        {
            case not null when !_overlayMan.HasOverlay<SerpentFocusOverlay>():
                _overlayMan.AddOverlay(_serpentOverlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_serpentOverlay);
                _serpentOverlay.ResetLight();
                break;
        }
    }

    private void UpdateOverlay(SerpentFocusComponent? tvComp)
    {
        _overlay.Comp = tvComp;

        switch (tvComp)
        {
            case { DrawOverlay: true } when !_overlayMan.HasOverlay<BaseSwitchableOverlay<SerpentFocusComponent>>():
                _overlayMan.AddOverlay(_overlay);
                break;
            case null or { DrawOverlay: false }:
                _overlayMan.RemoveOverlay(_overlay);
                break;
        }

        // Night vision overlay is prioritized
        _overlay.IsActive = !_overlayMan.HasOverlay<BaseSwitchableOverlay<NightVisionComponent>>();
    }
}
