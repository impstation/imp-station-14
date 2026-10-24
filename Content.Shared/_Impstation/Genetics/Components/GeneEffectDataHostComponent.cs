using Content.Shared._Impstation.Genetics.Genes.Effects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Components;

/// <summary>
/// Component that is in charge of storing Effect data for Gene Effects
///
/// This is useful if we have an affect that adds light to an Entity and 2 Genes use this effect
/// This component will allow those light effects to know of eachothers existence and mix accordingly
/// </summary>
[RegisterComponent]
public sealed partial class GeneEffectDataHostComponent : Component
{
    /// <summary>
    /// The Dictionary storing all our Effects & Data
    /// We store this as a dictionary of HashSets so effects can be grouped together for ease of access
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, HashSet<BaseGeneEffect>> StoredEffects = new Dictionary<string, HashSet<BaseGeneEffect>>();
}
