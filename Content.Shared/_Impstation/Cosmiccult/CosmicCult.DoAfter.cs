using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Cosmiccult;

[Serializable, NetSerializable]
public sealed partial class EventCosmicSiphonDoAfter : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class EventCosmicBlankDoAfter : SimpleDoAfterEvent { }
