using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity, intentionally "obvious details".
/// Like, that's it. It's basic -- all it does is add the line to the attached entity.
/// This is particularly used for assigning players unique examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ObviousExamineSystem), typeof(ObviousClothingSystem))]
public sealed partial class ObviousExamineComponent : Component
{
    /// <summary>
    /// The LocIds that will be added to the attached entity's examination.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]

    public HashSet<LocId> Lines { get; set; } = new();

}
