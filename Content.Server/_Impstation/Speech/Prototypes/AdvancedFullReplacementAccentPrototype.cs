using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Prototypes;
[Prototype]
public sealed class AdvancedFullReplacementAccentPrototype : IPrototype
{
    /// <summary>
    /// Accent ID and name.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; }=default!;

    /// <summary>
    /// List of word protoIDs and weights. the ID must have a corresponding word prototype.
    /// </summary>
    [DataField("words")]
    public Dictionary<ProtoId<AdvancedFullReplacementWordPrototype>, float> Words { get; private set; } = new();
}
