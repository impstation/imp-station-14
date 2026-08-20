using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Heretic.Prototypes;

[Serializable, NetSerializable, DataDefinition]
[Prototype]
public sealed partial class HereticRitualPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Used for the radial menu.
    /// </summary>
    [DataField] public string LocName = "heretic-ritual-unknown";

    /// <summary>
    /// Used for the radial menu.
    /// </summary>
    [DataField] public string LocDesc = string.Empty;

    /// <summary>
    /// How many items with certain tags are required for the ritual?
    /// </summary>
    [DataField] public Dictionary<ProtoId<TagPrototype>, int>? RequiredTags;

    /// <summary>
    /// What event will be raised on success?
    /// </summary>
    [DataField] public object? OutputEvent;

    /// <summary>
    /// What knowledge will be given on success?
    /// </summary>
    [DataField] public ProtoId<HereticKnowledgePrototype>? OutputKnowledge;

    /// <summary>
    /// What will be spawned on success?
    /// </summary>
    [DataField] public Dictionary<EntProtoId, int>? OutputItems;

    /// <summary>
    /// Icon for the radial menu.
    /// </summary>
    [DataField] public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new("_Goobstation/Heretic/amber_focus.rsi"), "icon");

    /// <summary>
    /// What the ritual does.
    /// </summary>
    [DataField] public List<SharedRitualBehaviorSystem> RitualBehavior;
}
