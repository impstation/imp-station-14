using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the red light replacement effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleRedLightSystem))]
public sealed partial class SlasherGameRuleRedLightComponent : Component
{
    [DataField]
    public int MinLights { get; set; } = 15;

    [DataField]
    public int MaxLights { get; set; } = 30;

    [DataField]
    public EntProtoId BulbPrototype { get; set; } = "LightTubeCrystalRed";
}
