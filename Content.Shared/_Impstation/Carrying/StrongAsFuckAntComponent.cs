using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Carrying;

/// <summary>
///     Override component which allows an entity to carry other entities regardless of density
///     and without being affected by slowdown.
/// </summary>
/// <remarks>
///     its my component and i can call it what i want -mq
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class StrongAsFuckAntComponent : Component { }
