namespace Content.Shared._Impstation.Containers;

/// <summary>
/// Emptys the container when the owner of this entity timed despawns.
/// </summary>
[RegisterComponent]
public sealed partial class EmptyContainerOnTimedDespawnComponent : Component
{
    /// <summary>
    /// The ID of the container to use.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId = string.Empty;
}
