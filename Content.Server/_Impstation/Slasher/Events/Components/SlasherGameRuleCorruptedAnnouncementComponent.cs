namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the corrupted announcement effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleCorruptedAnnouncementSystem))]
public sealed partial class SlasherGameRuleCorruptedAnnouncementComponent : Component
{
    /// <summary>Locale keys for the pool of corrupted announcement messages. One is picked at random.</summary>
    [DataField]
    public List<string> AnnouncementLocaleKeys { get; set; } = new()
    {
        "slasher-pulse-announcement-1",
        "slasher-pulse-announcement-2",
        "slasher-pulse-announcement-3",
    };
}
