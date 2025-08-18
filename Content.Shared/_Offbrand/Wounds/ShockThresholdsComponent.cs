using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ShockThresholdsSystem))]
public sealed partial class ShockThresholdsComponent : Component
{
    /// <summary>
    /// The status effects to apply depending on the amount of pain. Highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, EntProtoId> Thresholds;

    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentThresholdState;
}
