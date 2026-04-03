using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.CollectiveMind;

[RegisterComponent, NetworkedComponent]
public sealed partial class CollectiveMindComponent : Component
{
    /// <summary>
    /// This dictionary tracks collective mind membership. If an entry exists in this dictionary, the attached
    /// entity is in the collective mind denoted by the key, and details of its membership are described by the
    /// value.
    /// </summary>
    [DataField]
    public Dictionary<CollectiveMindPrototype, CollectiveMindMemberData> Minds = new();
}

/// <summary>
/// Stores data about the collective mind member.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class CollectiveMindMemberData
{
    [DataField(required: true)]
    public int MindId;

    /// <summary>
    /// The first ID number to assign to members of a collective mind. Subsequent IDs increment from this.
    /// </summary>
    public const int StartingId = 1;
}
