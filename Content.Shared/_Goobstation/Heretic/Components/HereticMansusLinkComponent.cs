using Robust.Shared.GameStates;

namespace Content.Shared.Heretic;

/// <summary>
/// Used for identifying if an entity has access to the mansus link collectivemind channel.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticMansusLinkComponent : Component
{
}
