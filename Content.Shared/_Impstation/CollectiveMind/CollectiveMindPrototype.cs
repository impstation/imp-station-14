using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CollectiveMind;

[Prototype, Serializable, NetSerializable]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public char KeyCode { get; private set; } = '\0';

    [DataField]
    public Color Color { get; private set; } = Color.Lime;
}
