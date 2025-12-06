using Content.Shared._Impstation.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Random;

namespace Content.Shared._Impstation.Damage.Systems;

/// <summary>
/// Applies randomized damage to an entity on init.
/// </summary>
public sealed class RandomDamageOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomDamageOnSpawnComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomDamageOnSpawnComponent component, MapInitEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        var randomizedDamage = new DamageSpecifier();

        foreach (var (damageType, baseDamage) in component.Damage.DamageDict)
        {
            // Each damage type is randomized
            var addition = _random.NextFloat(component.MinDamage, component.MaxDamage);
            var finalDamage = baseDamage + addition;
            randomizedDamage.DamageDict.Add(damageType, finalDamage);
        }

        _damageable.TryChangeDamage(
            (uid, damageable),
            randomizedDamage,
            ignoreResistances: component.IgnoreResistances,
            interruptsDoAfters: false);
    }
}
