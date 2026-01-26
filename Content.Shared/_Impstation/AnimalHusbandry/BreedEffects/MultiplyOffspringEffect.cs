using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._Impstation.AnimalHusbandry.BreedEffects;
public sealed partial class MultiplyOffspringEffect : BaseBreedEffect
{
    [DataField("extraOffspring")]
    public int ExtraOffspring = 0;

    private static EntitySystem? _entitySystem;

    public override void BirthEffect(EntityUid self, EntityUid offspring)
    {
        base.BirthEffect(self, offspring);
        if (_entManager == null) return;
        _entitySystem ??= IoCManager.Resolve<EntitySystem>();


    }

    private EntityUid? SpawnNewMob(EntityUid entity, EntityUid toSpawn)
    {
        if (_entManager == null) return null;
        if (!_entManager.TryGetComponent<TransformComponent>(entity, out var xform))
            return null;

        var newMob = Spawn(toSpawn, xform.Coordinates);
        return newMob;
    }
}
