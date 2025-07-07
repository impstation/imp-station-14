using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Anomaly;

namespace Content.Shared._Impstation.Anomaly.Effects.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedWallAnomalySystem))]
public sealed partial class WallSpawnAnomalyComponent : Component
{
    /// <summary>
    /// All types of floors spawns with their settings
    /// </summary>
    [DataField]
    public List<WallSpawnSettingsEntry> Entries = new();
}

[DataRecord]
public partial record struct WallSpawnSettingsEntry()
{
    /// <summary>
    /// The wall the anomaly spawns when replacing a normal wall.
    /// </summary>
    public EntProtoId Wall { get; set; } = default!;

    public AnomalySpawnSettings Settings { get; set; } = new();
}
