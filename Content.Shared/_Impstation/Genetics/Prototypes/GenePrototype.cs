using Content.Shared._Impstation.Genetics.Genes;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Prototypes;

[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    /// <summary>
    /// ID of our Prototype
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("components")]
    public ComponentRegistry _geneComponent = new();
}
