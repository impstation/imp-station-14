using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Tag;

namespace Content.Shared._Impstation.Physics.Components;

/// <summary>
/// Filters collisions between entities by checking for a valid component or tag.
/// Note: "Filter" in this context means to stop the check made for a collision, preventing entities from colliding with each other.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CollisionFilterComponent : Component
{
    /// <summary>
    /// Use a component to check for the filter. Can only check for one.
    /// Can be combined with RequiredTags.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? RequiredComponent;

    /// <summary>
    /// Tags to check for filtering the collision. Can check for multiple.
    /// Can be combined with RequiredComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>>? RequiredTags;

    /// <summary>
    /// Set whether all tags must match to filter or just any tag.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TagCheckMode TagCheckMode = TagCheckMode.All;

    /// <summary>
    /// This acts as a switch that flips the functionality of the component.
    /// If false (the default): All collisions act as normal with the exception of entities that have the required component/tags which ARE filtered.
    /// If true: All collisions are filtered with the exception of entities that have the required component/tags which ARE NOT filtered.
    /// Example with false: A device that when activated allows the user to walk through walls (filters collisions).
    /// Example with true: A barrier that only blocks projectiles (does not filter collisions).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FilterAll = false;
}

[Serializable, NetSerializable]
public enum TagCheckMode : byte
{
    /// <summary>
    /// Entity must have all of the tags.
    /// </summary>
    All,

    /// <summary>
    /// Entity just needs one of the tags.
    /// </summary>
    Any
}
