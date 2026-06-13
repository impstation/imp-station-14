using Content.Shared._EE.Damage.Events;
using Content.Shared._EE.Item.ItemToggle.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Shared._EE.Projectiles;

public sealed class EmbedPassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmbedPassiveDamageComponent, DamageOtherOnHitStartupEvent>(OnDamageOtherOnHitStartup);
        SubscribeLocalEvent<ItemToggleEmbedPassiveDamageComponent, ItemToggleDamageOtherOnHitStartupEvent>(OnItemToggleStartup);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, EmbedEvent>(OnEmbed);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, EmbedDetachEvent>(OnRemoveEmbed);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, ItemToggledEvent>(OnItemToggle);
    }

    /// <summary>
    ///   Inherit stats from DamageOtherOnHit.
    /// </summary>
    private void OnDamageOtherOnHitStartup(Entity<EmbedPassiveDamageComponent> ent, ref DamageOtherOnHitStartupEvent args)
    {
        if (ent.Comp.Damage.Empty)
            ent.Comp.Damage = args.Weapon.Comp.Damage * ent.Comp.ThrowingDamageMultiplier;
        Dirty(ent);
    }

    /// <summary>
    ///   Inherit stats from ItemToggleDamageOtherOnHit.
    /// </summary>
    private void OnItemToggleStartup(Entity<ItemToggleEmbedPassiveDamageComponent> ent, ref ItemToggleDamageOtherOnHitStartupEvent args)
    {
        if (!TryComp<EmbedPassiveDamageComponent>(ent, out var embedPassiveDamage) ||
            ent.Comp.ActivatedDamage != null ||
            !(args.Weapon.Comp.ActivatedDamage is { } activatedDamage))
            return;

        ent.Comp.ActivatedDamage = activatedDamage * embedPassiveDamage.ThrowingDamageMultiplier;
        Dirty(ent);
    }

    private void OnEmbed(Entity<EmbedPassiveDamageComponent> ent, ref EmbedEvent args)
    {
        if (ent.Comp.Damage.Empty || ent.Comp.Damage.GetTotal() == 0 ||
            !TryComp<MobStateComponent>(args.Embedded, out var mobState) ||
            !TryComp<DamageableComponent>(args.Embedded, out var damageable))
            return;

        ent.Comp.Embedded = args.Embedded;
        ent.Comp.EmbeddedDamageable = damageable;
        ent.Comp.EmbeddedMobState = mobState;
        ent.Comp.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);

        Dirty(ent);
    }

    private void OnRemoveEmbed(Entity<EmbedPassiveDamageComponent> ent, ref EmbedDetachEvent args)
    {
        ent.Comp.Embedded = null;
        ent.Comp.EmbeddedDamageable = null;
        ent.Comp.EmbeddedMobState = null;
        ent.Comp.NextDamage = TimeSpan.Zero;

        Dirty(ent);
    }

    /// <summary>
    ///   Used to update the EmbedPassiveDamageComponent component on item toggle.
    /// </summary>
    private void OnItemToggle(Entity<EmbedPassiveDamageComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<ItemToggleEmbedPassiveDamageComponent>(ent, out var itemTogglePassiveDamage))
            return;

        if (args.Activated && itemTogglePassiveDamage.ActivatedDamage is { } activatedDamage)
        {
            itemTogglePassiveDamage.DeactivatedDamage ??= ent.Comp.Damage;
            ent.Comp.Damage = activatedDamage;
        }
        else if (itemTogglePassiveDamage.DeactivatedDamage is { } deactivatedDamage)
            ent.Comp.Damage = deactivatedDamage;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EmbedPassiveDamageComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Embedded is null ||
                comp.EmbeddedDamageable is null ||
                comp.NextDamage > curTime || // Make sure they're up for a damage tick
                comp.EmbeddedMobState is null ||
                comp.EmbeddedMobState.CurrentState == MobState.Dead) // Don't damage dead mobs, they've already gone through too much
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            _damageable.TryChangeDamage((comp.Embedded.Value, comp.EmbeddedDamageable), comp.Damage, false, false);
        }
    }
}
