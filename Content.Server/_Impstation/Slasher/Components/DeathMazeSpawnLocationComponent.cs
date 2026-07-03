namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marker component for explicit spawn locations inside the Slasher death maze.
/// </summary>
[RegisterComponent]
public sealed partial class DeathMazeSpawnLocationComponent : Component
{
    /// <summary>
    /// Marks this marker as the maze center reference point (0,0).
    /// Center markers are excluded from crew spawn selection.
    /// </summary>
    [DataField("isCenter")]
    public bool IsCenter;
}
