using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity that wears item, for making things obvious.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WearerGetsExamineTextSystem))]
public sealed partial class WearerGetsExamineTextComponent : Component
{
    /// <summary>
    /// Shortname of the object. Used for the examine text on the object
    /// (warning/notifying the player that this will be visible on casual examine).
    /// </summary>
    [DataField]
    public string Thing = "thing";

    /// <summary>
    /// The LocId that will be added to any wearing entity's examination.
    /// </summary>
    [DataField("examineText", required: true)]
    public LocId ExamineOnWearer;

    /// <summary>
    /// Reference to the entity wearing this clothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    /// <summary>
    /// The string that is attached to this item's ExamineOnWearer.
    /// Typically shouldn't be redefined.
    /// </summary>
    [DataField]
    public LocId PrefixExamineOnWearer = "obvious-prefix-wearing";

    /// <summary>
    /// If true, an entity with this item in any slot (i.e. in pockets) will gain the examine text,
    /// instead of when just equipped as clothing.
    /// Should be used sparingly; this is a half-measure for lack of a special pin slot.
    /// </summary>
    [DataField]
    public bool PocketEvident;

    /// <summary>
    /// If true, the entity's description will inform examiners what others will see on the wearer (before they equip it).
    /// If the item is contraband, the item will also warn that displaying it may cause undue attention.
    /// Keep this false for good-natured jokes (i.e. the pride cloaks having funny, non-pride names)
    /// </summary>
    [DataField]
    public bool WarnExamine = true;
}
