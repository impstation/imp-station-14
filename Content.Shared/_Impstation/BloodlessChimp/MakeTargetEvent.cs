using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.BloodlessChimp;

[Serializable, NetSerializable]
public sealed class MakeTargetEvent(NetEntity target, TimeSpan cooldown, List<string> warnings) : EntityEventArgs
{
    public NetEntity Target = target;
    public TimeSpan CooldownTime = cooldown;
    public List<string> Warnings = warnings;
}
