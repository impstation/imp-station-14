using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicCorruptingComponent : Component
{
    /// <summary>
    /// Value the system uses to increment the corruption tick.
    /// </summary>
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan CorruptionValue = default!;

    /// <summary>
    /// The starting radius of the effect.
    /// </summary>
    [DataField]
    public float CorruptionRadius = 2;

    /// <summary>
    /// The maximum radius the corruption effect can grow to.
    /// </summary>
    [DataField]
    public float CorruptionMaxRadius = 50;

    /// <summary>
    /// The chance that a tile and/or wall is replaced.
    /// </summary>
    [DataField]
    public float CorruptionChance = 0.25f;

    /// <summary>
    /// Enables or disables the growth of the corruption radius.
    /// </summary>
    [DataField]
    public bool CorruptionGrowth = false;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should be running on this entity.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should ignore this component when it reaches max growth. Saves performance.
    /// </summary>
    [DataField]
    public bool AutoDisable = true;

    /// <summary>
    /// How much time between tile corruptions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CorruptionSpeed = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The tile we spawn when replacing a normal tile.
    /// </summary>
    [DataField]
    public EntProtoId ConversionTile = "FloorCosmicCorruption";

    /// <summary>
    /// The VFX entity we spawn when corruption occurs.
    /// </summary>
    [DataField]
    public EntProtoId TileConvertVFX = "CosmicFloorSpawnVFX";
}
