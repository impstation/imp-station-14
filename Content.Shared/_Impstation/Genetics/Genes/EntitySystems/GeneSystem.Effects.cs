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

        // If there are no registered effects then remove the Hashset
        if (data.StoredEffects[effectName].Count <= 0)
            data.StoredEffects.Remove(effectName);

        return true;
    }

    public void ProcessEffects(Entity<GeneEffectDataHostComponent> ent, string hash)
    {
        if (!ent.Comp.StoredEffects.ContainsKey(hash))
            return;

        foreach (BaseGeneEffect effect in ent.Comp.StoredEffects[hash])
        {

        }
    }
}
