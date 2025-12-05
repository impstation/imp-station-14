using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic.Prototypes;

[Prototype]
[Serializable, NetSerializable]
public sealed partial class HereticPathPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SoundSpecifier? AscensionSound { get; private set; } = new SoundPathSpecifier($"/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/ascend_blade.ogg");

    [DataField]
    public LocId Announcement { get; private set; } = "heretic-ascension-blade";

    [DataField]
    public List<ProtoId<HereticKnowledgePrototype>> Knowledge = [];
}
