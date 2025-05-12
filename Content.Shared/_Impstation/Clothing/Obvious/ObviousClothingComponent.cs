using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity that wears item, for making things obvious.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ObviousClothingSystem))]
public sealed partial class ObviousClothingComponent : Component
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
}
