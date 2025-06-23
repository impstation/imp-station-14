using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public char KeyCode { get; private set; } = '\0';

    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    [DataField]
    public List<string> RequiredComponents { get; set; } = [];

    [DataField]
    public List<string> RequiredTags { get; set; } = [];
}
