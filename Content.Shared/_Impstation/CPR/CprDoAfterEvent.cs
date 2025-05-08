using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CPR;

[Serializable, NetSerializable]
public sealed partial class CprDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
