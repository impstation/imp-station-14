using Content.Shared.Maps;
using Content.Shared.Spreader;
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
    /// Chance for it to play spawning audio
    /// </summary>
    [DataField]
    public float SpawningAudioChance = 0.2f;

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

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");
    [DataField]
    public SoundSpecifier SpawningCrystalSound = new SoundPathSpecifier("/Audio/_Impstation/Supermatter/cracking_crystal.ogg");
    [DataField]
    public EntProtoId SecondarySpawnPrototype = "CrystalBulbSpreader";
    [DataField]
    public ProtoId<ContentTileDefinition> MassPlating = "PlatingCrystalMass";
}
