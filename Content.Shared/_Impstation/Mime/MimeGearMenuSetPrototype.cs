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
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
