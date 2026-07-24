namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the phantom mass casualty crew monitor effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleMassCasualitySystem))]
public sealed partial class SlasherGameRuleMassCasualityComponent : Component
{
    [DataField]
    public int MinEntries { get; set; } = 10;

    [DataField]
    public int MaxEntries { get; set; } = 15;

    [DataField]
    public float DurationSeconds { get; set; } = 60f;

    [DataField]
    public int DeadWeight { get; set; } = 3;

    [DataField]
    public int CritWeight { get; set; } = 4;

    [DataField]
    public int BadWeight { get; set; } = 5;
}
