using Content.Shared._Impstation.Genetics.Genes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Components;

/// <summary>
/// Holds all the Data for a mobs Genes
/// </summary>
public abstract partial class SharedGeneHostComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, BaseGenePrototype> _genes = new Dictionary<string, BaseGenePrototype>();

    /// <summary>
    /// Where the entity currently is on the gene scale
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int _geneScaleValue = 0;

    /// <summary>
    /// These are all the values for where the segments of the Gene Scale begin
    /// Riskzone - Genetic damage slowly accumulates 
    /// Dangerzone - Genetic damage slowly accumulates + new genes begin developing
    /// Max - Death
    /// </summary>
    [DataField("RiskzoneBounds"), ViewVariables(VVAccess.ReadOnly)]
    public Vector2i _geneScaleRiskZone = new Vector2i(-100, 100);

    [DataField("DangerzoneBounds"), ViewVariables(VVAccess.ReadOnly)]
    public Vector2i _geneScaleDangerZone = new Vector2i(-150, 150);

    [DataField("GeneScaleMax"), ViewVariables(VVAccess.ReadOnly)]
    public Vector2i _geneScaleMax = new Vector2i(-300, 300);
}
