using Content.Server.Atmos.EntitySystems;
using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Genes.Effects;
using Content.Shared._Impstation.Genetics.Genes.Effects.Negative;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Genes.Effects.Negative;

/// <summary>
/// Lights the entity up like the sun when applied
/// </summary>
public sealed partial class OnFireGeneEffect : BaseGeneEffect
{
    private static FlammableSystem _flammableSystem = default!;

    /// <summary>
    /// How many fire stacks we're giving the entity, this controls how powerful the fire is.
    /// </summary>
    /// <remark>
    /// Why does it control that
    /// </remark>
    [DataField("firestacks", serverOnly: true)]
    public int _fireStacks = 100;

    /// <summary>
    /// Controls if we ignite the entity or purely modify existing fire stacks
    /// Good for if we only want to effect something already on fire.
    /// </summary>
    [DataField("ignite", serverOnly: true)]
    public bool _ignite = true;

    /// <summary>
    /// Applies fire to an Entity.
    /// The Chromosomes used for this effect are:
    ///     Powerbooster:
    ///         Doubles the fire stacks applied.
    /// </summary>
    /// <param name="entity">The entity we're lighting up</param>
    /// <param name="chromosomes">The entities Chromosomes</param>
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
