using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;
[Prototype("advancedFullReplacementAccent")]
public sealed class AdvancedFullReplacementAccentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; }=default!;

    /// <summary>
    /// Words to pick from and their weights.
    /// </summary>
    [DataField("words")]
    public Dictionary<ProtoId<AdvancedFullReplacementWordPrototype>, float> Words { get; private set; } = new();
}
