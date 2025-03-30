using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.MindlessClone;

public abstract partial class SharedMindlessCloneComponent : Component
{

}

[Serializable, NetSerializable]
public sealed partial class MindlessCloneSayDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed partial class MindlessCloneDelayDoAfterEvent : SimpleDoAfterEvent
{

}
