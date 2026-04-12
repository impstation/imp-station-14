using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the ghost orb vent pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleGhostOrbSystem))]
public sealed partial class SlasherGameRuleGhostOrbComponent : Component
{
    /// <summary>Orb prototype launched from vents.</summary>
    [DataField]
    public EntProtoId OrbPrototype { get; set; } = "SlasherGhostOrb";

    /// <summary>How many station vents to sample before selecting launch points.</summary>
    [DataField]
    public int VentSampleCount { get; set; } = 5;

    /// <summary>How many ghost orbs to launch from the sampled vents.</summary>
    [DataField]
    public int OrbCount { get; set; } = 3;

    /// <summary>Initial launch speed in meters per second.</summary>
    [DataField]
    public float OrbSpeed { get; set; } = 12f;
}
