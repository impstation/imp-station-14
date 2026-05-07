using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CrystalMass;

/// <summary>
/// Handles spreading of crystal mass
/// </summary>
[RegisterComponent]
public sealed partial class CrystalMassComponent : Component
{
    /// <summary>
    /// Chance for it to spread
    /// </summary>
    [DataField]
    public float SpreadChance = 0.25f;

    /// <summary>
    /// Number of sprite variations for crystal mass
    /// </summary>
    public int SpriteVariants = 5;

    /// <summary>
    /// Chance for a bulb to spawn when spread
    /// </summary>
    [DataField]
    public float BulbChance = 0.1667f;

    /// <summary>
    /// If the crystal mass is a bulb
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsBulb = false;

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");
    [DataField]
    public SoundSpecifier SpawningCrystalSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/cracking_crystal.ogg");
    [DataField]
    public EntProtoId CrystalMassPrototype = "CrystalMass";
    [DataField]
    public EntProtoId CrystalBulbPrototype = "CrystalBulb";
}
