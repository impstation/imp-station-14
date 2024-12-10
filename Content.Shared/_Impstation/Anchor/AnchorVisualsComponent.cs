using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Anchor;

/// <summary>
/// When added to an entity, sets <seealso cref="AnchorVisuals.Anchored"/>
/// appearance data to the current anchored state.
///
/// Intended for use with GenericVisualizer.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnchorVisualsComponent : Component
{
}
