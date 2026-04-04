namespace Content.Server._Impstation.AdminGhost;

/// <summary>
/// Signifies the entity as an admin ghost, currently only used for ghost warps.
/// </summary>
[RegisterComponent]
public sealed partial class AdminGhostComponent : Component
{
    /// <summary>
    /// Whether the entity shows up in the ghost warp list.
    /// </summary>
    [DataField]
    public bool Warpable = true;
}
