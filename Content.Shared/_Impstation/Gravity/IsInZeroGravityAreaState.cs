using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

[Serializable, NetSerializable]
public sealed partial class IsInZeroGravityAreaState : ComponentState
{
    public IsInZeroGravityAreaState(bool weightless)
    {
        IsWeightless = weightless;
    }
    public bool IsWeightless;
}
