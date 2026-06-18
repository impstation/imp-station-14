using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// When the round will if all the command are dead (Incase they are in space)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CommandCheck;

    /// <summary>
    /// The amount of time between each check for command check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The time it takes after the last head is killed for the shuttle to arrive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(5);

    //imp datafields below

    /// <summary>
    /// The threshold of converted players that will prompt an automatic alert level change.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BlueThreshold = 0.3f;

    /// <summary>
    /// The alert level triggered by a threshold of players being converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string AlertLevel = "blue";

    /// <summary>
    /// The announcement id triggered by a threshold of players being converted.
    /// This is not the string itself, but the ID, e.g. it will search for station-event-sleeper-agents
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string RuleId = "sleeper-agents";

    /// <summary>
    /// Whether the game rule has already triggered a conversion announcement.
    /// </summary>
    [DataField]
    public bool AnnouncementDone = false;
}
