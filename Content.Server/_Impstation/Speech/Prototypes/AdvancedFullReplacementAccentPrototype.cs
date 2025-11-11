using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;
[Prototype("AdvancedFullReplacementAccent")]
public sealed class AdvancedFullReplacementAccentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; }=default!;

    /// <summary>
    /// Words to pick from and their weights.
    /// </summary>
    [DataField("Words")]
    public Dictionary<AdvancedWordFullReplacementWordPrototype, float> Words { get; private set; } = new();
}
