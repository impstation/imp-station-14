using Robust.Shared.Audio;

namespace Content.Server._Impstation.CrystalMass;

/// <summary>
/// Handles spreading of crystal mass
/// </summary>
[RegisterComponent]
public sealed partial class CrystalMassComponent : Component
{
    /// <summary>
    /// Chance to spread whenever an edge spread is possible.
    /// </summary>
    [DataField]
    public float SpreadChance = 0.25f;

    /// <summary>
    /// Number of sprite variations for crystal mass
    /// </summary>
    [DataField]
    public int SpriteVariants = 5;

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
