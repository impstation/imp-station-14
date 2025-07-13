using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Storage.Events;

[Serializable, NetSerializable]
public sealed partial class DelayedSpawnItemOnUseDoAfterEvent : SimpleDoAfterEvent;
