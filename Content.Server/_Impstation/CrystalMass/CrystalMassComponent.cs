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
    /// If the crystal mass has cleared its tile of entities
    /// </summary>
    [DataField]
    public bool ClearedTile = false;

    /// <summary>
    /// The time to clear the crystal mass tile on
    /// </summary>
    [ViewVariables]
    public TimeSpan ClearTime;

    /// <summary>
    /// Delay until the tile is cleared
    /// </summary>
    [DataField]
    public TimeSpan ClearTileDelay = TimeSpan.FromSeconds(0.025f);
    /// <summary>
    /// Chance for it to spread
    /// </summary>
    [DataField]
    public float SpreadChance = 0.5f;

    /// <summary>
    /// Chance for it to play spawning audio
    /// </summary>
    [DataField]
    public float SpawningAudioChance = 0.2f;


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
    /// If the crystal mass should set its appearance on startup
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool StartupAppearance = true;

    /// <summary>
    /// If the crystal mass is a bulb
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsBulb = false;

    /// <summary>
    /// Crystal bulb pointlight radius
    /// </summary>
    [DataField]
    public float BulbRadius = 10f;

    /// <summary>
    /// Crystal bulb pointlight energy
    /// </summary>
    [DataField]
    public float BulbEnergy = 2f;

    /// <summary>
    /// Crystal bulb pointlight color
    /// </summary>
    [DataField]
    public Color BulbColor = Color.FromHex("#FBFF23");

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");
    [DataField]
    public SoundSpecifier SpawningCrystalSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/cracking_crystal.ogg");
    [DataField]
    public EntProtoId CrystalMassPrototype = "CrystalMassSpreader";
    [DataField]
    public EntProtoId CrystalBulbPrototype = "CrystalBulbSpreader";
}
