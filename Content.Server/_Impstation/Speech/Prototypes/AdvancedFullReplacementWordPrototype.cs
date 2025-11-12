using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;
[Prototype("advancedFullReplacementWord")]

public sealed class AdvancedFullReplacementWordPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; }=default!;

    [DataField("lengthMatch")]
    public bool LengthMatch { get; private set; }=false;

    [DataField("replacement")]
    public string Replacement { get; private set; } = default!;
    [DataField("prefix")]
    public string Prefix { get; private set; } =default!;
    [DataField("suffix")]
    public string Suffix { get; private set; }=default!;
}
