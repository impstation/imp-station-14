using Robust.Shared.GameStates;
using Content.Shared._Impstation.TraitorFlavor; // imp

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a syndicate traitor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TraitorRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    ///     Imp Edit: Hold issuer to display on round end for traitors.
    /// </summary>
    [DataField]
    public TraitorEmployerPrototype? Employer;
}
