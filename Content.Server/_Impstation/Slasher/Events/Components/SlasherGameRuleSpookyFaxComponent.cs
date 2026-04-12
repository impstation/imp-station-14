using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the spooky fax effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleSpookyFaxSystem))]
public sealed partial class SlasherGameRuleSpookyFaxComponent : Component
{
    /// <summary>Minimum number of random non-captain faxes to target.</summary>
    [DataField]
    public int MinRandomFaxes { get; set; } = 2;

    /// <summary>Maximum number of random non-captain faxes to target.</summary>
    [DataField]
    public int MaxRandomFaxes { get; set; } = 4;

    /// <summary>Paper title used for spooky faxes.</summary>
    [DataField]
    public string PrintoutTitle { get; set; } = "priority transmission";

    /// <summary>Locale keys for spooky fax body messages. One key is chosen per pulse and reused for every selected fax.</summary>
    [DataField]
    public List<string> MessageLocaleKeys { get; set; } = new()
    {
        "slasher-pulse-fax-message-1",
        "slasher-pulse-fax-message-2",
        "slasher-pulse-fax-message-3",
    };
}
