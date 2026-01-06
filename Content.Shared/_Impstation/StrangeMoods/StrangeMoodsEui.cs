using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StrangeMoods;

[Serializable, NetSerializable]
public sealed class StrangeMoodsEuiState(HashSet<SharedMood> allSharedMoods, List<StrangeMood> moods, SharedMood? sharedMood, NetEntity target) : EuiStateBase
{
    public HashSet<SharedMood> AllSharedMoods { get; } = allSharedMoods;
    public List<StrangeMood> Moods { get; } = moods;
    public SharedMood? SharedMood { get; } = sharedMood;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class StrangeMoodsSaveMessage(List<StrangeMood> moods, ProtoId<SharedMoodPrototype>? sharedMood, NetEntity target) : EuiMessageBase
{
    public List<StrangeMood> Moods { get; } = moods;
    public ProtoId<SharedMoodPrototype>? SharedMood { get; } = sharedMood;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class StrangeMoodsGenerateRequestMessage(NetEntity target) : EuiMessageBase
{
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class StrangeMoodsGenerateSendMessage(StrangeMood mood) : EuiMessageBase
{
    public StrangeMood Mood { get; } = mood;
}

[Serializable, NetSerializable]
public sealed class StrangeMoodsSharedRequestMessage(ProtoId<SharedMoodPrototype>? mood) : EuiMessageBase
{
    public ProtoId<SharedMoodPrototype>? Mood { get; } = mood;
}


[Serializable, NetSerializable]
public sealed class StrangeMoodsSharedSendMessage(SharedMood? mood) : EuiMessageBase
{
    public SharedMood? Mood { get; } = mood;
}
