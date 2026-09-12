using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers; /// imp add

namespace Content.Shared.Trigger.Systems;

public sealed class DamageOnTriggerSystem : XOnTriggerSystem<DamageOnTriggerComponent>
{
    [Dependency] private readonly Damage.Systems.DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; /// imp add


    protected override void OnTrigger(Entity<DamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // Imp add start
        // Override the normal target if we target the container
        if (ent.Comp.TargetContainer)
        {
            // damage whoever is wearing this clothing item
            if (!_container.TryGetContainingContainer(ent.Owner, out var container))
                return;

            target = container.Owner;
        }
        // Imp add end

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner);
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
