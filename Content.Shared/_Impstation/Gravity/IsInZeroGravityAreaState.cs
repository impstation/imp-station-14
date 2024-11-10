using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

[Serializable, NetSerializable]
public sealed partial class IsInZeroGravityAreaState : ComponentState
{
    public IsInZeroGravityAreaState(int areaFingerprint)
    {
        AreaFingerprint = areaFingerprint;
    }
    public int AreaFingerprint;
}
