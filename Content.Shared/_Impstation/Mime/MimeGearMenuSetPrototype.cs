using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Mime;

/// <summary>
/// A prototype that defines a set of items and visuals in a specific starter set for mimes
/// This should probably be made generic since this is copied from chaplain's (and thief)'s versions of this
/// </summary>
[Prototype("mimeGearMenuSet")]
public sealed partial class MimeGearMenuSetPrototype : IPrototype
{
    /// <summary>
    /// The ID of the prototype.
    /// </summary>
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the gear set.
    /// </summary>
    [DataField] public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The description of the gear set.
    /// </summary>
    [DataField] public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The sprite to use to represent it.
    /// </summary>
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    /// <summary>
    /// What is spawned when someone selects and accepts it.
    /// </summary>
    [DataField] public List<EntProtoId> Content = new();
}
