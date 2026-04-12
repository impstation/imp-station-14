namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the door malice effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleDoorMaliceSystem))]
public sealed partial class SlasherGameRuleDoorMaliceComponent : Component
{
    /// <summary>Radius (in tiles) around each player to look for doors.</summary>
    [DataField]
    public float DoorRadius { get; set; } = 6f;

    /// <summary>Total number of toggle cycles to perform.</summary>
    [DataField]
    public int CycleCount { get; set; } = 3;

    /// <summary>Fraction of connected crew sampled each cycle (minimum 1 when players are present).</summary>
    [DataField]
    public float PlayerFractionPerCycle { get; set; } = 0.33f;

    /// <summary>Seconds between toggle cycles.</summary>
    [DataField]
    public float CycleIntervalSeconds { get; set; } = 0.8f;

    /// <summary>Runtime: doors toggled during the pulse.</summary>
    public readonly HashSet<EntityUid> AffectedDoors = new();

    /// <summary>Runtime: how many toggle cycles have been completed.</summary>
    public int CyclesDone { get; set; }

    /// <summary>Runtime: game time at which the next toggle cycle fires.</summary>
    public TimeSpan NextCycleAt { get; set; }
}
