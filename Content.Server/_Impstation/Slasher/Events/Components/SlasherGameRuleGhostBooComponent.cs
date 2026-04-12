namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the global ghost Boo pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleGhostBooSystem))]
public sealed partial class SlasherGameRuleGhostBooComponent : Component
{
    /// <summary>Maximum number of Boo reactions to trigger in one pulse.</summary>
    [DataField]
    public int MaxTargets { get; set; } = 40;
}
