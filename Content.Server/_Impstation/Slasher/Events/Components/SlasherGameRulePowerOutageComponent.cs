namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the power outage effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRulePowerOutageSystem))]
public sealed partial class SlasherGameRulePowerOutageComponent : Component
{
    /// <summary>Warning delay before APC breakers are cut, in seconds.</summary>
    [DataField]
    public float ShutdownDelay { get; set; } = 5f;

    /// <summary>How long the power stays out, in seconds.</summary>
    [DataField]
    public float Duration { get; set; } = 30f;

    /// <summary>Runtime: game time when the outage starts.</summary>
    public TimeSpan OutageStartsAt { get; set; }

    /// <summary>Runtime: station selected for this outage.</summary>
    public EntityUid? AffectedStation { get; set; }

    /// <summary>Runtime: game time at which to end the outage.</summary>
    public TimeSpan EndsAt { get; set; }

    /// <summary>Runtime: true once APCs have been shut down.</summary>
    public bool OutageStarted { get; set; }

    /// <summary>Runtime: APCs that were toggled off and must be restored on end.</summary>
    public readonly List<EntityUid> Unpowered = new();
}
