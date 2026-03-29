using Content.Shared._Impstation.BloodlessChimp.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Shared._Impstation.BloodlessChimp;

public sealed class DropItemOnDamageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropItemOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<DropItemOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if(args is { DamageIncreased: false, DamageDelta: null })
            return;
        if (!TryComp<MobStateComponent>(ent, out var mobState))
            return;
        var dropItems = 0;
        foreach (var damageType in ent.Comp.requiredTypes.DamageDict)
        {
            if (args.DamageDelta!.DamageDict.TryGetValue(damageType.Key, out var damage) &&
                damage >= damageType.Value &&
                ent.Comp.AllowedStates.Contains(mobState.CurrentState) &&
                _random.Prob(ent.Comp.DropChance))
            {
                if (ent.Comp.DropOne)
                {
                    dropItems=1;
                    break;
                }

                dropItems += damage.Int()/damageType.Value.Int();
            }

        }

        for (var i = 0; i < dropItems; i++)
        {
            PredictedSpawnNextToOrDrop(ent.Comp.ItemToDrop, ent);
        }

    }
}
