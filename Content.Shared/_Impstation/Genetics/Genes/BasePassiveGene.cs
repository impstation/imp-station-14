using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

/// <summary>
/// Passive Genes are Genes that subscribe to events to apply their effect OR they
/// can use an update function to steadily do something to a mob
///
/// Examples include Fire vulnerability
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BasePassiveGene : BaseGenePrototype
{

}
