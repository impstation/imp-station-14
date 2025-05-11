using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tag.TagOnBreakage;

/// <summary>
/// Component that tags itself if the entity ever calls BreakageEventArgs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(TagOnBreakageSystem))]
public sealed partial class TagOnBreakageComponent : Component
{
    /// <summary>
    ///  Tags that will be added to the entity when it breaks.
    /// </summary>
    [DataField("tags", required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public ProtoId<TagPrototype>[] Tags;
    /// <summary>
    /// Bool to determine if the system should replace the original tags or not (if any exist). Defaults to false.
    /// </summary>
    [DataField("replaceOldTags"), ViewVariables(VVAccess.ReadOnly)]
    public bool ReplaceTags = false;
    /// <summary>
    /// Bool to determine if the system should bromg back the original tags if repaired. Does nothing if replaceOldTags isn't
    /// set to true. Defaults to false.
    /// </summary>
    [DataField("reTagOnRepair"), ViewVariables(VVAccess.ReadOnly)]
    public bool ReTagOnRepair = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTagged = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<TagPrototype>[] OldTags;
}
