using Content.Server._Impstation.Objectives.Systems;
using Content.Shared._Impstation.TraitorFlavor;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Objectives.Components;

[RegisterComponent, Access(typeof(EmployerRequirementSystem))]
public sealed partial class EmployerRequirementComponent : Component
{
    /// <summary>
    /// A whitelist of employers whose traitors can get this objective.
    /// </summary>
    [DataField("whitelist")]
    public List<ProtoId<TraitorEmployerPrototype>>? Whitelist;

    /// <summary>
    /// A blacklist of employers whose traitors cannot get this objective.
    /// </summary>
    [DataField("blacklist")]
    public List<ProtoId<TraitorEmployerPrototype>>? Blacklist;
}
