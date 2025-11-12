using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;
[Prototype("advancedFullReplacementWord")]

public sealed class AdvancedFullReplacementWordPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; }=default!;

    /// <summary>
    /// if this word tries to match the length of the word it is replacing
    /// </summary>
    [DataField("lengthMatch")]
    public bool LengthMatch { get; private set; }=false;

    /// <summary>
    /// the loc ID of the word that is used as a replacement, is repeated if length match is true.
    /// </summary>
    [DataField("replacement")]
    public string Replacement { get; private set; } = default!;

    /// <summary>
    /// the loc ID of the prefix for the word, goes at the start. Only used if the word is a length match word.
    /// </summary>
    [DataField("prefix")]
    public string Prefix { get; private set; } =default!;
    /// <summary>
    /// the loc ID of the suffix for the word, goes at the end. Only used if the word is a length match word.
    /// </summary>
    [DataField("suffix")]
    public string Suffix { get; private set; }=default!;
}
