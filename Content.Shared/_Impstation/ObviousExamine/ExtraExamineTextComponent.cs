using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity, intentionally "obvious details".
/// Like, that's it. It's basic -- all it does is add the lines to the attached entity.
/// This is particularly used for assigning players unique examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ExtraExamineTextSystem), typeof(WearerGetsExamineTextSystem))]
public sealed partial class ExtraExamineTextComponent : Component
{
    /// <summary>
    /// The LocIds that will be added to the attached entity's examination.
    ///
    /// The key is the examine text that will be added, and the value is optional prefix text.
    /// We have to put the specific examine text in front so that keys are unique and so we can remove text when necessary.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]

    public Dictionary<LocId, LocId> Lines { get; set; } = new();

}
