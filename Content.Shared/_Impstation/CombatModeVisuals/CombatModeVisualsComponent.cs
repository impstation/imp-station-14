using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CombatModeVisuals;

/// <summary>
/// Allows the use of unique sprites for combat mode
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CombatModeVisualsComponent : Component
{
    [DataField]
    public bool HideBaseLayer;
}
