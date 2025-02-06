using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Prototypes;
// Content.Shared/_Impstation/CosmicCult/Prototypes/GlyphPrototype.cs
[Prototype]
public sealed partial class GlyphPrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId Tooltip;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;
}
