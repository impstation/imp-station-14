using Content.Shared.Store;
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
    public List<ProtoId<HereticKnowledgePrototype>> Knowledge = [];
}
