using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Speech.EntitySystems;
[RegisterComponent]

public sealed partial class AdvancedFullReplacementAccentComponent: Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<AdvancedFullReplacementAccentPrototype>), required: true)]
    public string Accent = default!;

}

/// <summary>
///
/// </summary>
[Serializable, DataDefinition]
public partial record struct CachedWord
{
    [DataField]
    public bool LengthMatch;

    [DataField]
    public string Word;

    [DataField]
    public string Prefix="";

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
