using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.EquipmentForceFacing;

/// <summary>
/// Only used for griffy suit. Forces the equippee to face north when their mobstate changes to critical or incapacitated
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EquipmentForceFacingComponent : Component
{

}
