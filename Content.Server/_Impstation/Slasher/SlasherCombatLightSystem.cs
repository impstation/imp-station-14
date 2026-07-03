using Content.Server._Impstation.Slasher.Components;
using Content.Shared.CombatMode;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Manages the Slasher's combat light implant lifecycle and combat-mode light toggling.
/// </summary>
public sealed class SlasherCombatLightSystem : EntitySystem
{
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

    /// <summary>
    /// Subscribes implant grant/removal and combat-mode light toggle handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherCombatLightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SlasherCombatLightComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlasherCombatLightComponent, CombatModeChangedEvent>(OnCombatModeChanged);
        SubscribeLocalEvent<SubdermalImplantComponent, EntityTerminatingEvent>(OnImplantTerminating);
    }

    /// <summary>
    /// Grants the combat light implant and activates it immediately if the Slasher is already in combat mode.
    /// </summary>
    private void OnStartup(Entity<SlasherCombatLightComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.ImplantUid is { } existingImplant)
        {
            if (Exists(existingImplant))
                return;

            ent.Comp.ImplantUid = null;
        }

        var implantUid = _implants.AddImplant(ent.Owner, ent.Comp.ImplantPrototype);
        if (implantUid == null)
            return;

        ent.Comp.ImplantUid = implantUid;

        if (TryComp<CombatModeComponent>(ent.Owner, out var combat)
            && combat.IsInCombatMode
            && TryComp<UnpoweredFlashlightComponent>(implantUid.Value, out var lightComp))
        {
            _unpoweredFlashlight.SetLight((implantUid.Value, lightComp), true, quiet: true);
        }
    }

    /// <summary>
    /// Removes the combat light implant when the component is shut down.
    /// Force-deletes the implant entity if normal removal is not possible.
    /// </summary>
    private void OnShutdown(Entity<SlasherCombatLightComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ImplantUid is not { } implantUid)
        {
            return;
        }

        if (TryComp<ImplantedComponent>(ent.Owner, out var implanted))
            _implants.ForceRemove((ent.Owner, implanted), implantUid);
        else if (Exists(implantUid))
            Del(implantUid);

        ent.Comp.ImplantUid = null;
    }

    /// <summary>
    /// Toggles the combat light on or off in response to Slasher combat mode changes.
    /// Clears the implant reference if the implant entity no longer exists.
    /// </summary>
    private void OnCombatModeChanged(Entity<SlasherCombatLightComponent> ent, ref CombatModeChangedEvent args)
    {
        if (ent.Comp.ImplantUid is not { } implantUid)
            return;

        if (!Exists(implantUid))
        {
            ent.Comp.ImplantUid = null;
            return;
        }

        if (!TryComp<UnpoweredFlashlightComponent>(implantUid, out var lightComp))
            return;

        _unpoweredFlashlight.SetLight((implantUid, lightComp), args.Enabled, quiet: true);
    }

    /// <summary>
    /// Clears the tracked implant UID on the host entity when the implant itself is deleted.
    /// Prevents stale references from blocking future implant grants.
    /// </summary>
    private void OnImplantTerminating(Entity<SubdermalImplantComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.ImplantedEntity is not { } implantedUid)
            return;

        if (!TryComp<SlasherCombatLightComponent>(implantedUid, out var combatLight))
            return;

        if (combatLight.ImplantUid == ent.Owner)
            combatLight.ImplantUid = null;
    }
}
