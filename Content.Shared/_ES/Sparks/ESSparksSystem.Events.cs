using Content.Shared._ES.Sparks.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Projectiles;
using Content.Shared.Trigger;
using Robust.Shared.Spawners;

namespace Content.Shared._ES.Sparks;

/// <summary>
/// Handles all the events for <see cref="ESSparksSystem"/>.
/// </summary>
public sealed partial class ESSparksSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSparkOnHitComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<ESSparkOnItemToggleComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<ESSparkOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ESSparkOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
        SubscribeLocalEvent<ESSparkOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    /// <summary>
    /// If an entity with <see cref="ESSparkOnHitComponent"/> is damaged and the change in damage is above the set threshold,
    /// release sparks.
    /// </summary>
    private void OnDamaged(Entity<ESSparkOnHitComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        if (args.DamageDelta.GetTotal() < ent.Comp.Threshold)
            return;

        DoSparks(ent, user: args.Origin);
    }

    /// <summary>
    /// Release sparks when an entity with <see cref="ESSparkOnItemToggleComponent"/> is toggled.
    /// </summary>
    private void OnItemToggled(Entity<ESSparkOnItemToggleComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated != ent.Comp.ActivatedSpark)
            return;
        DoSparks(ent, user: args.User);
    }

    /// <summary>
    /// Release sparks when a projectile with <see cref="ESSparkOnProjectileHitComponent"/> hits something.
    /// </summary>
    private void OnProjectileHit(Entity<ESSparkOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        DoSparks(ent, args.Shooter);
    }

    /// <summary>
    /// Release sparks when an entity with <see cref="ESSparkOnDespawnComponent"/> despawns.
    /// </summary>
    private void OnDespawn(Entity<ESSparkOnDespawnComponent> ent, ref TimedDespawnEvent args)
    {
        DoSparks(ent);
    }

    /// <summary>
    /// Release sparks when an entity with <see cref="ESSparkOnTriggerComponent"/> is triggered.
    /// </summary>
    private void OnTrigger(Entity<ESSparkOnTriggerComponent> ent, ref TriggerEvent args)
    {
        DoSparks(ent);
    }
}
