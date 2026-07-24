using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Slasher role marker.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class SlasherRoleComponent : Component
{
    /// <summary>
    /// Actions granted while this component is active.
    /// Configured in YAML.
    /// </summary>
    [DataField]
    public List<EntProtoId> Actions { get; set; } = new();

    /// <summary>Spawned action entities to clean up on removal.</summary>
    public List<EntityUid> ActionEntities { get; } = new();
}
