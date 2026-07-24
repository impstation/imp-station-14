using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Components;

/// <summary>
/// Spawns entities in an area centered on this entity when it dies.
/// Generic utility component usable by any role or mob.
/// </summary>
[RegisterComponent]
public sealed partial class OnDeathEntitySpawnComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn on each valid tile.
    /// </summary>
    [DataField]
    public EntProtoId EntityPrototype { get; set; } = "Smoke";

    /// <summary>
    /// Radius in tiles around the death tile. 1 = 3x3, 2 = 5x5.
    /// </summary>
    [DataField]
    public int Radius { get; set; } = 1;
}
