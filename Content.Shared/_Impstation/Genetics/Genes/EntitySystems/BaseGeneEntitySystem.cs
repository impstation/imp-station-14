using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes.Effects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.EntitySystems;

[Virtual]
public partial class BaseGeneEntitySystem : EntitySystem
{
    public virtual void OnGeneAdded(Entity<BaseGeneComponent> entity)
    {

    }

    public virtual void OnGeneRemoved(Entity<BaseGeneComponent> entity)
    {

    }

    public virtual void ApplyEffects(Entity<BaseGeneComponent> entity)
    {
        if (!TryComp<SharedGeneHostComponent>(entity.Owner, out var host))
            return;

        foreach(BaseGeneEffect effect in entity.Comp.NeutralEffects)
        {
            effect.ApplyGeneEffect((entity.Owner, host), entity.Comp.ActiveChromosomes);
        }

        foreach (BaseGeneEffect effect in entity.Comp.PositiveEffects)
        {
            effect.ApplyGeneEffect((entity.Owner, host), entity.Comp.ActiveChromosomes);
        }
    }
}
