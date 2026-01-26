using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Shared._Impstation.AnimalHusbandry.BreedEffects;
public sealed partial class GenerateExplosionEffect : BaseBreedEffect
{
    [Dependency] public SharedExplosionSystem _explosionSystem = default!;

    public override void BreedEffect(EntityUid self, EntityUid partner, IEntityManager? entitySystem = null)
    {
        if (_explosionSystem == null)
            return;

        _explosionSystem.QueueExplosion(
            self,
            "Default",
            20f,
            10f,
            10f
        );
    }
}
