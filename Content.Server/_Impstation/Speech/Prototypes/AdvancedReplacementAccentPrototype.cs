using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;
[Prototype("AdvancedReplacementAccent")]
public sealed class AdvancedReplacementAccentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; }=default!;

    /// <summary>
    /// Words to pick from and their weights.
    /// </summary>
    [DataField("Words")]
    public Dictionary<AdvancedWordReplacementWordPrototype, float> Words { get; private set; } = new();
}
