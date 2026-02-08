using Content.Server.Atmos.EntitySystems;
using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Genes.Effects;
using Content.Shared._Impstation.Genetics.Genes.Effects.Negative;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Genes.Effects.Negative;

public sealed partial class OnFireGeneEffect : BaseGeneEffect
{
    private static FlammableSystem _flammableSystem = default!;

    [DataField("firestacks", serverOnly: true)]
    public int _fireStacks = 100;

    [DataField("ignite", serverOnly: true)]
    public bool _ignite = true;

    public override void ApplyGeneEffect(Entity<SharedGeneHostComponent> entity, Dictionary<Chromosome, bool> chromosomes)
    {
        base.ApplyGeneEffect(entity, chromosomes);

        _flammableSystem ??= _entityManager.System<FlammableSystem>();

        var stackToApply = _fireStacks;

        if (chromosomes[Chromosome.PowerBooster] == true)
            stackToApply *= 2;

        _flammableSystem.AdjustFireStacks(entity, stackToApply, null, _ignite);
    }
}
