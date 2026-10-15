using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Components;


[RegisterComponent, Virtual]
public partial class IonizeGeneComponent : BaseSetGeneComponent
{
    /// <summary>
    /// If you are immune to radiation from this gene
    /// </summary>
    [DataField("selfImmune")]
    public bool RadImmune = true;

    /// <summary>
    /// How strong the radiation this gene emits is
    /// </summary>
    [DataField("radStrength")]
    public int RadStrength = 1;

    /// <summary>
    /// If the entity was already immune to radiation before we got here
    /// </summary>
    public bool PrevImmune = false;
}
