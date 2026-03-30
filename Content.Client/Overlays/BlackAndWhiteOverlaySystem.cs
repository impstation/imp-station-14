using Content.Shared._DV.CCVars;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Overlays;

public sealed partial class BlackAndWhiteOverlaySystem : EquipmentHudSystem<BlackAndWhiteOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private BlackAndWhiteOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        //imp addition begins
        SubscribeLocalEvent<BlackAndWhiteOverlayComponent, ComponentInit>(OnBWVisionInit);
        SubscribeLocalEvent<BlackAndWhiteOverlayComponent, ComponentShutdown>(OnBWVisionShutdown);
        SubscribeLocalEvent<BlackAndWhiteOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlackAndWhiteOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, DCCVars.NoVisionFilters, OnNoVisionFiltersChanged);
        //imp addition ends
    }
# region imp additions

    private void OnPlayerDetached(Entity<BlackAndWhiteOverlayComponent> ent, ref LocalPlayerDetachedEvent args) //imp add
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<BlackAndWhiteOverlayComponent> ent, ref LocalPlayerAttachedEvent args) //imp add
    {
        if (!_cfg.GetCVar(DCCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnBWVisionShutdown(Entity<BlackAndWhiteOverlayComponent> ent, ref ComponentShutdown args) //imp add
    {
        if (ent.Owner == _playerMan.LocalEntity)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnBWVisionInit(Entity<BlackAndWhiteOverlayComponent> ent, ref ComponentInit args) //imp add
    {
        if (ent.Owner == _playerMan.LocalEntity && !_cfg.GetCVar(DCCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnNoVisionFiltersChanged(bool enabled) //imp add
    {
        if (enabled)
            _overlayMan.RemoveOverlay(_overlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }
    #endregion

    protected override void UpdateInternal(RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

}
