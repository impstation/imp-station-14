using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Impstation.AttackRevenge;

/// <summary>
///     Handles entities that have their attack attempt on another entity cancelled,
///     unless the entity recieving damage has already attacked an entity with the corresponding key
/// </summary>
public sealed class AttackRevengeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttackRevengeComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<AttackRevengeComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    /// <summary>
    ///     Adds the corresponding revenge key to an entity which attacks this entity
    /// </summary>
    private void OnDamageChanged(Entity<AttackRevengeComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin is not { } attacker)
            return;

        if (!HasComp<MobStateComponent>(attacker))
            return;

        EnsureComp<AttackMemoryComponent>(attacker, out var memory);
        memory.Keys.Add(ent.Comp.Key);
        Dirty(attacker, memory);
    }

    /// <summary>
    ///     Cancels any attack this entity makes on another living entity,
    ///     unless that entity has the corresponding revenge key
    /// </summary>
    private void OnAttackAttempt(Entity<AttackRevengeComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is not { } target || !HasComp<MobStateComponent>(target))
            return;

        if (!TryComp<AttackMemoryComponent>(target, out var memory) || !memory.Keys.Contains(ent.Comp.Key))
            args.Cancel();
    }
}
