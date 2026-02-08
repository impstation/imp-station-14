using Content.Shared._Impstation.Genetics.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Effects;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseGeneEffect
{
    protected static IEntityManager _entityManager = default!;

    public virtual void ApplyGeneEffect(Entity<SharedGeneHostComponent> entity, Dictionary<Chromosome, bool> chromosomes) { _entityManager ??= IoCManager.Resolve<IEntityManager>(); }
}
