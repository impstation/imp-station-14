using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.BountyHunter;

/// <summary>
/// Component for the WeaponRecall action.
/// Used for marking a held weapon and recalling it back into your hand with second action use.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedWeaponRecallSystem))]
public sealed partial class WeaponRecallComponent : Component
{
    /// <summary>
    /// The name the action should have while an entity is marked.
    /// </summary>
    [DataField]
    public LocId? WhileMarkedName = "item-recall-marked-name";

    /// <summary>
    /// The description the action should have while an entity is marked.
    /// </summary>
    [DataField]
    public LocId? WhileMarkedDescription = "item-recall-marked-description";

    /// <summary>
    /// The name the action starts with.
    /// This shouldn't be set in yaml.
    /// </summary>
    [DataField]
    public string? InitialName;

    /// <summary>
    /// The description the action starts with.
    /// This shouldn't be set in yaml.
    /// </summary>
    [DataField]
    public string? InitialDescription;

    /// <summary>
    /// The entity currently marked to be recalled by this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MarkedEntity;
}
