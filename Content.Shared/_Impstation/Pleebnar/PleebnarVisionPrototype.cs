using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Pleebnar;

/// <summary>
///     Handles replacing speech verbs and other conditional chat modifications like bolding or font type depending
///     on punctuation or by directly overriding the prototype.
/// </summary>
[Prototype]
public sealed partial class PleebnarVisionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     Loc string for the vision
    /// </summary>
    [DataField("visionString", required: true)]
    public LocId VisionString = string.Empty;
    /// <summary>
    ///     Loc string for the vision name
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;
}
