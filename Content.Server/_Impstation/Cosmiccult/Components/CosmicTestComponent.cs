using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class CosmicTestComponent : Component
{
    [DataField]
    public float CorruptionRadius = 10;

    /// <summary>
    ///     Length of the cooldown in between tile corruptions.
    /// </summary>
    [DataField]
    public float CorruptionCooldown = 0.75f;

    /// <summary>
    ///     Counter for tile corruption.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CorruptionAccumulator = 0;

    [DataField]
    public List<ProtoId<EntityPrototype>> CultTile = new()
    {
        "FloorCosmicSmooth",
        "FloorCosmicHalf",
        "FloorCosmicSplit",
        "FloorCosmicNotched",
    };

    [DataField]
    public EntProtoId TileConvertVFX = "CosmicFloorSpawnEffect";
}
