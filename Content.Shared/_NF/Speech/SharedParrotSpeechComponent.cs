using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Speech;

public abstract partial class SharedParrotSpeechComponent : Component // imp. added this
{

}

[Serializable, NetSerializable]
public sealed partial class ParrotSpeechDoAfterEvent : SimpleDoAfterEvent // imp
{

}
