using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

/// <summary>
/// Active Genes are ones that use Actions to activate
/// Think things like fireball
/// </summary>
[ImplicitDataDefinitionForInheritors, RegisterComponent]
[Virtual]
public partial class BaseActiveGeneComponent : BaseGeneComponent
{

    //[Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    /// <summary>
    /// The action this Gene will add & remove from the mob it is applied to
    /// </summary>
    [DataField("geneAction"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId _action;

    /// <summary>
    /// The ID for the applied action
    /// </summary>
    public EntityUid _actionId = EntityUid.Invalid;
}
