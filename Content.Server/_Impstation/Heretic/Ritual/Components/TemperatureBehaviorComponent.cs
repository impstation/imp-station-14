namespace Content.Server._Impstation.Heretic.Ritual;

[RegisterComponent]
public sealed partial class TemperatureBehaviorComponent : Component
{
    /// <summary>
    /// Min temp in celsius
    /// </summary>
    [DataField] public float MinThreshold { get; set; } = 0f;

    /// <summary>
    /// Max temp in celsius
    /// </summary>
    [DataField] public float MaxThreshold { get; set; } = float.PositiveInfinity;
}
