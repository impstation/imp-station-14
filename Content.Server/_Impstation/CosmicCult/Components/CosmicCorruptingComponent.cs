using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicCorruptingComponent : Component
{
    [DataField]
    public float CorruptionRadius = 10;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan CorruptionValue = default!;

    /// <summary>
    /// How much time between tile corruptions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CorruptionSpeed = TimeSpan.FromSeconds(5);

    [DataField]
    public List<ProtoId<EntityPrototype>> CultTile = new()
    {
        "FloorCosmicSmooth",
        "FloorCosmicHalf",
        "FloorCosmicSplit",
        "FloorCosmicNotched",
    };

    [DataField]
    public EntProtoId TileConvertVFX = "CosmicFloorSpawnVFX";
}
