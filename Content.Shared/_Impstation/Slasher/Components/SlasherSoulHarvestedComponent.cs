using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Tracks temporary immunity to soul-fragment harvesting.
/// Victims with this component cannot be harvested again until the timer expires, even if they stay on the hook.
/// Also exposes a status icon for Slasher viewers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlasherSoulHarvestedComponent : Component
{
    /// <summary>
    /// World time when this harvest lockout expires.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ExpiresAt { get; set; }

    /// <summary>
    /// Icon shown to slasher viewers while this lockout marker exists.
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "SlasherSoulHarvestedFaction";
}
