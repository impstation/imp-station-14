using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

[Serializable, NetSerializable]
public sealed partial class IsInZeroGravityAreaState : ComponentState
{
    public IsInZeroGravityAreaState(bool isWeightless)
    {
        IsWeightless = isWeightless;
    }
    public bool IsWeightless;
}
