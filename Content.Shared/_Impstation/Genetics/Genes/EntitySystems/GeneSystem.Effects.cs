using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes.Effects;
using Content.Shared._Impstation.Genetics.Systems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.EntitySystems;

public sealed partial class GeneSystem : SharedGeneSystem
{
    /// <summary>
    /// Adds a Gene effect to the Entity's GeneEffectDataHostComponent Dictionary
    /// This is necessary for Effects that want to communicate with one another
    /// </summary>
    /// <typeparam name="T">The effect Type</typeparam>
    /// <param name="ent">The entity whose DataHost we're adding to</param>
    /// <param name="effect">The effect we're adding</param>
    /// <returns>True if the gene was successfully added to the storage</returns>
    public bool AddToEffectStorage<T>(EntityUid ent, T effect) where T : BaseGeneEffect
    {
        if (!TryComp<GeneEffectDataHostComponent>(ent, out var data))
            return false;

        var effectName = effect.GetType().ToString();

        if (data.StoredEffects.ContainsKey(effectName))
            data.StoredEffects.Add(effectName, new HashSet<BaseGeneEffect>());

        data.StoredEffects[effectName].Add(effect);

        ProcessEffects((ent, data), effectName);

        return true;
    }

    /// <summary>
    /// Removes a GeneEffect from the Entity's GeneEffectDataHostComponent Dictionary
    /// </summary>
    /// <typeparam name="T">The effect Type</typeparam>
    /// <param name="ent">The entity whose DataHost we're adding to</param>
    /// <param name="effect">The effect we're adding</param>
    /// <returns>True if the gene was successfully removed from the storage</returns>
    public bool RemoveFromEffectStorage<T>(EntityUid ent, T effect) where T : BaseGeneEffect
    {
        if (!TryComp<GeneEffectDataHostComponent>(ent, out var data))
            return false;

        var effectName = effect.GetType().ToString();

        if (!data.StoredEffects.ContainsKey(effectName) ||
            !data.StoredEffects[effectName].Contains(effect))
            return false;

        data.StoredEffects[effectName].Remove(effect);

        // If there are no registered effects then remove the Hashset, otherwise update the
        // effects that are still attached
        if (data.StoredEffects[effectName].Count <= 0)
            data.StoredEffects.Remove(effectName);
        else
            ProcessEffects((ent, data), effectName);

        return true;
    }

    /// <summary>
    /// Runs through all the Genes in a given hash and gets the first to recalculate
    /// the attached effect.
    ///
    /// This only TRIES running through all if the first gene for whatever reason cannot
    /// calculate. If it can, then the loop ends.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="hash"></param>
    public void ProcessEffects(Entity<GeneEffectDataHostComponent> ent, string hash)
    {
        if (!ent.Comp.StoredEffects.ContainsKey(hash))
            return;

        foreach (BaseGeneEffect effect in ent.Comp.StoredEffects[hash])
        {
            if (effect.ProcessMultiEffects(ent.Owner, hash))
                return;
        }
    }

    /// <summary>
    /// Gets a particular hash of effects from an entity
    /// </summary>
    /// <param name="ent">The entity getting its effects collected</param>
    /// <param name="hash">The particular hash being requested</param>
    /// <returns>The hashset if it can be found, else it will return a new <see cref="HashSet<BasegeneEffect>"></returns>
    public HashSet<BaseGeneEffect> TryGetGeneHash(Entity<GeneEffectDataHostComponent> ent, string hash)
    {
        return ent.Comp.StoredEffects.ContainsKey(hash) ? ent.Comp.StoredEffects[hash] : new HashSet<BaseGeneEffect>();
    }
}
