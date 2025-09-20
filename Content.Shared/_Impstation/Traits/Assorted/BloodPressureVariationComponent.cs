using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Traits.Assorted;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodPressureVariationComponent : Component
{
    /// <summary>
    /// Multiplier to apply to systolic blood pressure.
    /// This is applied after blood pressure deviation.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 SystolicMultiplier = 1;

    /// <summary>
    /// Multiplier to apply to diastolic blood pressure.
    /// This is applied after blood pressure deviation.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 DiastolicMultiplier = 1;
}
