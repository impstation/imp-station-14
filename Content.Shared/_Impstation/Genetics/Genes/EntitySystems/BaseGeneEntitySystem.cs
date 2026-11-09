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

    /// <summary>
    /// Goes through the Positive, Neutral & Negative effect Hashes to apply their effects
    /// </summary>
    /// <param name="entity">The entity suffering the effects</param>
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

    /// <summary>
    /// Goes through the Positive, Neutral & Negative effect Hashes to remove their effects
    /// </summary>
    /// <param name="entity">The entity being freed of the effects</param>
    public virtual void RemoveEffects(Entity<BaseGeneComponent> entity)
    {
        if (!TryComp<SharedGeneHostComponent>(entity.Owner, out var host))
            return;

        foreach (BaseGeneEffect effect in entity.Comp.NeutralEffects)
        {
            effect.RemoveGeneEffect((entity.Owner, host), entity.Comp.ActiveChromosomes);
        }

        foreach (BaseGeneEffect effect in entity.Comp.PositiveEffects)
        {
            effect.RemoveGeneEffect((entity.Owner, host), entity.Comp.ActiveChromosomes);
        }
    }
}
