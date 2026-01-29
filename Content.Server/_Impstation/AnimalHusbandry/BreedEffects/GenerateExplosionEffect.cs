using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Server._Impstation.AnimalHusbandry.BreedEffects;

/// <summary>
/// Generates an explosion whenever a mob breeds or gives birth depending on which one is set
/// </summary>
public sealed partial class GenerateExplosionEffect : BaseBreedEffect
{
    private static SharedExplosionSystem? _explosionSystem;

    public override void BreedEffect(EntityUid self, EntityUid partner)
    {
        base.BreedEffect(self, partner);
        if (_entManager == null) return;

        _explosionSystem ??= _entManager.System<SharedExplosionSystem>();

        _explosionSystem.QueueExplosion(
            self,
            "Default",
            20f,
            10f,
            10f
        );
    }

    public override void BirthEffect(EntityUid self, EntityUid offspring)
    {
        base.BirthEffect(self, offspring);
        if (_entManager == null) return;

        _explosionSystem ??= _entManager.System<SharedExplosionSystem>();

        _explosionSystem.QueueExplosion(
            self,
            "Default",
            20f,
            10f,
            10f
        );
    }
}
