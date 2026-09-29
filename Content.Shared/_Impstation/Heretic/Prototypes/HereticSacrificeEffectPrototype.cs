using Robust.Shared.Prototypes;
namespace Content.Shared.Heretic.Prototypes;

[Prototype("hereticSacrificeEffect")]
public sealed partial class HereticSacrificeEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The components that get added to the player when they get this effect.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; private set; } = default!;

    /// <summary>
    /// The message describing what happened to them.
    /// </summary>
    [DataField]
    public LocId Message { get; private set; } = string.Empty;
}
