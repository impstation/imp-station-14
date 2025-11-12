using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Speech.EntitySystems;
[RegisterComponent]

public sealed partial class AdvancedFullReplacementAccentComponent: Component
{
    /// <summary>
    /// ID of the accent being used
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AdvancedFullReplacementAccentPrototype> Accent = default!;

}

/// <summary>
/// struct used to store a cached version of the word prototypes. Meaning that it stores the localized string instead of the ID
/// </summary>
[Serializable, DataDefinition]
public partial record struct CachedWord
{
    /// <summary>
    /// if this word tries to match the length of the word it is replacing
    /// </summary>
    [DataField]
    public bool LengthMatch;

    /// <summary>
    /// Word that is used as a replacement, is repeated if length match is true. localized.
    /// </summary>
    [DataField]
    public string Word;

    /// <summary>
    /// Prefix for the word, goes at the start. Only used if the word is a length match word. localized.
    /// </summary>
    [DataField]
    public string Prefix="";

    /// <summary>
    /// Suffix for the word, goes at the end. Only used if the word is a length match word. localized.
    /// </summary>
    [DataField]
    public string Suffix="";


    public CachedWord(bool lengthMatch, string word, string prefix="", string suffix="")
    {
        LengthMatch = lengthMatch;
        Word = word;
        Prefix = prefix;
        Suffix = suffix;
    }
}
