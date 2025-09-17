using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StrangeMoods;

[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class SharedMood
{
    /// <summary>
    /// The prototype this mood was created from.
    /// </summary>
    [DataField]
    public ProtoId<SharedMoodPrototype>? ProtoId;

    /// <summary>
    /// The list of strange moods attached to the shared mood.
    /// </summary>
    [DataField]
    public List<StrangeMood> Moods = [];

    /// <summary>
    /// The dataset that the shared moods will pull from.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DatasetPrototype> Dataset;

    /// <summary>
    /// The amount of shared moods to be given.
    /// </summary>
    [DataField]
    public int Count = 1;
}

[Prototype]
public sealed partial class SharedMoodPrototype : SharedMood, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID
    {
        get => ProtoId ?? "";
        set => ProtoId = value;
    }
}
