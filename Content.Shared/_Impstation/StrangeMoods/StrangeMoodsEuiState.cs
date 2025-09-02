using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StrangeMoods;

[Serializable, NetSerializable]
public sealed class StrangeMoodsEuiState(List<StrangeMood> moods, ProtoId<SharedMoodPrototype>? sharedMood, NetEntity target) : EuiStateBase
{
    public List<StrangeMood> Moods { get; } = moods;
    public ProtoId<SharedMoodPrototype>? SharedMood { get; } = sharedMood;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class StrangeMoodsSaveMessage(List<StrangeMood> moods, ProtoId<SharedMoodPrototype>? sharedMood, NetEntity target) : EuiMessageBase
{
    public List<StrangeMood> Moods { get; } = moods;
    public ProtoId<SharedMoodPrototype>? SharedMood { get; } = sharedMood;
    public NetEntity Target { get; } = target;
}
