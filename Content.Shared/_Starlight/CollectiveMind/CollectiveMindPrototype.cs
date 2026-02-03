using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.CollectiveMind;

[Prototype("collectiveMind")]
[Serializable, NetSerializable]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("keycode", required: true)]
    public char KeyCode;

    [DataField]
    public Color Color = Color.Lime;

    [DataField]
    public List<string> RequiredComponents = [];

    [DataField]
    public List<ProtoId<TagPrototype>> RequiredTags = [];
}
