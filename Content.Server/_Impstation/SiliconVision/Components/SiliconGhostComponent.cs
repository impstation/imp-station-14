using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.SiliconVision;

/// <summary>
/// Represents something that can only be seen by an entity with SiliconVisionComponent.
/// </summary>
[RegisterComponent]
public sealed partial class SiliconGhostComponent : Component { } 
