using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the anomaly burst effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleAnomalyBurstSystem))]
public sealed partial class SlasherGameRuleAnomalyBurstComponent : Component
{
    [DataField]
    public EntProtoId FleshSpawnerPrototype { get; set; } = "SlasherAnomalySpawnerFlesh";

    [DataField]
    public EntProtoId ShadowSpawnerPrototype { get; set; } = "SlasherAnomalySpawnerShadow";

    [DataField]
    public int FleshCount { get; set; } = 2;

    [DataField]
    public int ShadowCount { get; set; } = 1;
}
