using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Explosion;

[RegisterComponent]
public sealed partial class ExplodeOnMeleeHitComponent : Component
{
    /// <summary>
    /// If this is true, the weapon will explode when it hits another entity while thrown.
    /// </summary>
    [DataField]
    public bool ExplodeOnThrow = true;

    /// <summary>
    /// If this is set, the melee weapon will turn into this entity after exploding.
    /// </summary>
    [DataField]
    public EntProtoId? ReplaceWith;

    /// <summary>
    /// Used by code to determine whether the replacement item has already been spawned, to prevent duplicate spawns.
    /// </summary>
    [DataField]
    public bool HasSpawnedItem;
}
