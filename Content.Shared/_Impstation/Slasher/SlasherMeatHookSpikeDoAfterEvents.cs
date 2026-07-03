using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Raised when the hooking do-after for mounting a victim on a meathook completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherSpikeHookDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised when the unhooking do-after for removing a victim from a meathook completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherSpikeUnhookDoAfterEvent : SimpleDoAfterEvent;
