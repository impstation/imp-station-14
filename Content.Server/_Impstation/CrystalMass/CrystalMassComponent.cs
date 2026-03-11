using Robust.Shared.Audio;

namespace Content.Server._Impstation.CrystalMass;

/// <summary>
/// Handles spreading of crystal mass
/// </summary>
[RegisterComponent]
public sealed partial class CrystalMassComponent : Component
{
    /// <summary>
    /// Directions available for crystal mass to spread to.
    /// </summary>
    [DataField]
    public List<Direction> AvailableDirs = new()
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    /// <summary>
    /// If the crystal mass can spread.
    /// </summary>
    [DataField]
    public bool Spreading = true;

    /// <summary>
    /// Number of sprite variations for crystal mass
    /// </summary>
    [DataField]
    public int SpriteVariants = 5;

    /// <summary>
    /// Next time to spread at
    /// </summary>
    [DataField]
    public TimeSpan NextSpread = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Chance for a bulb to spawn when spread
    /// </summary>
    [DataField]
    public float BulbChance = 0.1667f;

    /// <summary>
    /// If the crystal mass is a bulb
    /// </summary>
    [DataField]
    public bool IsBulb = false;

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");

    [DataField]
    public SoundSpecifier CrackingCrystalSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/cracking_crystal.ogg");
}
