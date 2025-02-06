using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Prototypes;
// Content.Shared/_Impstation/CosmicCult/Prototypes/InfluencePrototype.cs
[Prototype]
public sealed partial class InfluencePrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    // I don't know what type means! This should maybe be like a prototype?
    [DataField(required: true)]
    public LocId InfluenceType;

    [DataField(required: true)]
    public int Cost;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;
}
