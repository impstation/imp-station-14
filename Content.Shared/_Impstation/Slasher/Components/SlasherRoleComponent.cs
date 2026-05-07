using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Marker component identifying Slasher antag entities.
/// Used by slasher-specific interactions that need quick role checks.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class SlasherRoleComponent : Component
{
    /// <summary>
    /// Actions granted to Slashers while this component is active.
    /// Configured on the antag selection component list so the loadout lives in YAML.
    /// </summary>
    [DataField]
    public List<EntProtoId> Actions { get; set; } = new();

    /// <summary>Runtime tracking of spawned action entities for cleanup on removal.</summary>
    public List<EntityUid> ActionEntities { get; } = new();
}
