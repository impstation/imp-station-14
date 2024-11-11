using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Gravity;

[Serializable, NetSerializable]
public sealed partial class IsInZeroGravityAreaState : ComponentState
{
    public IsInZeroGravityAreaState(ulong areaFingerprint)
    {
        AreaFingerprint = areaFingerprint;
    }

    /// <summary>
    /// <para>
    /// Bitfield that represents the <seealso cref="ZeroGravityAreaComponent"/>s acting
    /// on this entity.
    /// </para>
    ///
    /// See also: <seealso cref="Content.Shared._Impstation.Gravity.SharedZeroGravityAreaComponent.PredictIndex">SharedZeroGravityAreaComponent.PredictIndex</seealso>
    /// </summary>
    public ulong AreaFingerprint;
}
