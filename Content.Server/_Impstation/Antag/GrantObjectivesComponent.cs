using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Antag;

/// <summary>
/// Gives the a player an objective when they take control of a ghost role entity
/// </summary>
[RegisterComponent]
public sealed partial class GrantObjectivesComponent : Component
{
    /// <summary>
    /// The objective to be given to players.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ObjectiveComponent>> Objectives = new();
}
