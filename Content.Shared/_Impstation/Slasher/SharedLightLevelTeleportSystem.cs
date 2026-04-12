using Content.Shared._Impstation.Slasher.Components;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Shared subscription base for light-level teleport actions and their do-after completion events.
/// </summary>
public abstract class SharedLightLevelTeleportSystem : EntitySystem
{
    /// <summary>
    /// Registers shared teleport action and do-after event subscriptions.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightLevelTeleportActionComponent, SlasherDarkStepEvent>(OnTeleport);
        SubscribeLocalEvent<LightLevelTeleportActionComponent, SlasherDarkStepDoAfterEvent>(OnTeleportDoAfter);
    }

    /// <summary>
    /// Called when a light-level teleport action is activated.
    /// </summary>
    /// <param name="ent">Teleport action entity and configuration.</param>
    /// <param name="args">World-target action event data.</param>
    protected virtual void OnTeleport(Entity<LightLevelTeleportActionComponent> ent, ref SlasherDarkStepEvent args)
    {
    }

    /// <summary>
    /// Called when the light-level teleport do-after completes or is cancelled.
    /// </summary>
    /// <param name="ent">Teleport action entity and configuration.</param>
    /// <param name="args">Do-after event data.</param>
    protected virtual void OnTeleportDoAfter(Entity<LightLevelTeleportActionComponent> ent, ref SlasherDarkStepDoAfterEvent args)
    {
    }
}