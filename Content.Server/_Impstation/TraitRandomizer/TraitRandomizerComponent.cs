//using

namespace Content.Server._Impstation.TraitRandomizer;

[RegisterComponent]
public sealed partial class TraitRandomizerComponent : Component
{
    [DataField]
    public int MaxTraits = 3;

    [DataField]
    public int MinTraits = 1;

    /// <summary>
    /// whether or not to roll accents. default false
    /// </summary>
    [DataField]
    public bool Accents;

    /// <summary>
    /// whether or not to roll fonts. default false
    /// </summary>
    [DataField]
    public bool Fonts;

    /// <summary>
    /// whether or not to roll quirks. default false
    /// </summary>
    [DataField]
    public bool Quirks;

    /// <summary>
    /// whether or not to roll disabilities. default false
    /// </summary>
    [DataField]
    public bool Disabilities;
}
