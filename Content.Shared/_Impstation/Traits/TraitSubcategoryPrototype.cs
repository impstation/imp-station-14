using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Traits;

/// <summary>
/// Traits subcategory. Prevents players from taking redundant traits for free points (e.g. blindness and colorblindness).
/// </summary>
[Prototype]
public sealed partial class TraitSubcategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Name of the trait category displayed in the UI
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;
}
