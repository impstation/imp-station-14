using Content.Shared._Impstation.Genetics.Genes;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Prototypes;

/// <summary>
/// The Prototype for all Genes as described through YAML
/// </summary>
[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    /// <summary>
    /// ID of our Prototype
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The Component that the Gene uses
    /// </summary>
    [DataField("components")]
    public ComponentRegistry _geneComponent = new();
}
