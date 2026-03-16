using Content.Shared._Impstation.Genetics.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Effects;

/// <summary>
/// The base of all Gene Effects
/// These are fun little things you can apply to Genes through YAML so we keep them seperate
/// for that purpose. 
/// They don't really need to be a Component or EntitySystem as they
/// generally are called from the EntitySystem for each Gene.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseGeneEffect
{
    protected static IEntityManager _entityManager = default!;

    /// <summary>
    /// The effect we apply onto the Entity
    /// </summary>
    /// <param name="entity">The Entity suffering this effect</param>
    /// <param name="chromosomes">The related Chromosomes for the Effect to use</param>
    public virtual void ApplyGeneEffect(Entity<SharedGeneHostComponent> entity, Dictionary<Chromosome, bool> chromosomes) { _entityManager ??= IoCManager.Resolve<IEntityManager>(); }
}
