using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Mind;

/// <summary>
/// Adds components to an entity's mind when they get one, deletes itself after.
/// </summary>
[RegisterComponent]
public sealed partial class AddMindComponentComponent : Component
{
    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public bool RemoveExisting = true;
}
