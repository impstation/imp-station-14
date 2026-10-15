using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Radiation.Events;

namespace Content.Shared._Impstation.Genetics.Systems;

public sealed partial class GeneSystem : SharedGeneSystem
{
    /// <summary>
    /// Every time an entity takes radiation damage, roll to see if they mutate based on the amount of damage
    /// and their innate mutation chance
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    public void Irradiated(Entity<SharedGeneHostComponent> entity, ref OnIrradiatedEvent args)
    {
        if (!_entityManager.TryGetComponent<DamageableComponent>(entity, out var damage))
            return;

        var mutateOdds = entity.Comp._mutateChance * (damage.Damage.DamageDict["Radiation"].Value / 100);

        if (_random.NextFloat(0, 100) < mutateOdds)
            AddGeneRandom(entity);
    }
}
