using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Mind;

/// <summary>
/// Adds components to an entity's mind when they get one, deletes itself after.
/// </summary>
[RegisterComponent]
public sealed partial class AddMindComponentComponent : Component
{
    /// <summary>
    /// The components to add to the mind.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Whether to remove existing components of the same type before adding the new ones.
    /// </summary>
    [DataField]
    public bool RemoveExisting = true;
}
