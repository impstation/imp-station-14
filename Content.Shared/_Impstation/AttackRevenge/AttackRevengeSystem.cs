using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Impstation.AttackRevenge;

public sealed class AttackRevengeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttackRevengeComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<AttackRevengeComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

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

    private void OnAttackAttempt(Entity<AttackRevengeComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is not { } target || !HasComp<MobStateComponent>(target))
            return;

        if (!TryComp<AttackMemoryComponent>(target, out var memory) || !memory.Keys.Contains(ent.Comp.Key))
            args.Cancel();
    }
}
