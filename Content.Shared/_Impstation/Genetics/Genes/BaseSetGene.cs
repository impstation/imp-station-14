using Content.Shared._Impstation.Genetics.Systems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

/// <summary>
/// Set Genes are Genes that modify a component when they are applied & removed
/// </summary>
[ImplicitDataDefinitionForInheritors, RegisterComponent]
[Virtual]
public partial class BaseSetGeneComponent : BaseGeneComponent
{

}
