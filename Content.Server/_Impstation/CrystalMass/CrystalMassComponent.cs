using Content.Shared.Maps;
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
    /// Chance for it to not spread
    /// </summary>
    [DataField]
    public float SpreadChance = 0.5f;

    /// <summary>
    /// Chance for it to play spawning audio.
    /// Used to reduce the concurrent amount of audio playing
    /// </summary>
    [DataField]
    public float SpawningAudioChance = 0.5f;

    /// <summary>
    /// Chance for a secondary entity to spawn instead when spread
    /// </summary>
    [DataField]
    public float SecondaryChance;

    /// <summary>
    /// If the crystal mass should set its appearance on startup
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool StartupAppearance;

    /// <summary>
    /// Number of sprite variations for crystal mass
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int SpriteVariants = 5;

    /// <summary>
    /// If the crystal mass is a light source
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsLight;

    /// <summary>
    /// pointlight radius after clearing tile
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float LightRadius = 10f;

    /// <summary>
    /// pointlight energy after clearing tile
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float LightEnergy = 2f;

    /// <summary>
    /// pointlight color after clearing tile
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Color LightColor = Color.FromHex("#FBFF23");

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/supermatter.ogg")
    {
        Params = AudioParams.Default
            .WithVolume(-3f)
    };

    [DataField]
    public SoundSpecifier SpawningCrystalSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/cracking_crystal.ogg")
    {
        Params = AudioParams.Default
            .WithVolume(1f)
            .WithVariation(0.25f)
    };

    [DataField]
    public EntProtoId SecondarySpawnPrototype = "CrystalBulbSpreader";

    [DataField]
    public ProtoId<ContentTileDefinition> MassPlating = "PlatingCrystalMass";
}
