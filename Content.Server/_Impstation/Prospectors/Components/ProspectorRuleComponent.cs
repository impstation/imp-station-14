using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Server.RoundEnd;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.Prospectors.Components;

/// <summary>
/// Component for the ProspectorRuleSystem that should store gameplay info.
/// </summary>
[RegisterComponent, Access(typeof(ProspectorRuleSystem))]
public sealed partial class ProspectorRuleComponent : Component
{
    /// <summary>
    /// What happens if all Prospectors are killed or arrested.
    /// </summary>
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// When the round will if all Prospectors are killed or arrested.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ProspCheck;

    /// <summary>
    /// Sender for shuttle call.
    /// </summary>
    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call.
    /// </summary>
    [DataField]
    public string RoundEndTextShuttleCall = "prospector-elimination-shuttle-call";

    /// <summary>
    /// Text for announcement.
    /// </summary>
    [DataField]
    public string RoundEndTextAnnouncement = "prospector-elimination-announcement";

    /// <summary>
    /// Time for emergency shuttle arrival.
    /// </summary>
    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(4);

    /// <summary>
    /// The amount of time between each check for prospector check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField] public HashSet<EntityUid> Prospectors = new();
    [DataField] public bool WinLocked = false;
    [DataField] public WinType WinType = WinType.CrewMinor;
}

// ProspectorRuleComponent

public enum WinType : byte
{
    /// <summary>
    ///    Prospectors major win. The Gang managed to steal a hefty sum of spesos and their objective targets.
    /// </summary>
    ProspMajor,
    /// <summary>
    ///    Prospectors minor win. The Gang stole the target amount of spesos or the objective targets, but did not do both.
    /// </summary>
    ProspMinor,
    /// <summary>
    ///     Neutral. The Gang didn't complete any of their objectives, but none of them were killed or arrested.
    /// </summary>
    Neutral,
    /// <summary>
    ///     Crew minor win. Some of the Gang were killed or arrested in their attempt.
    /// </summary>
    CrewMinor,
    /// <summary>
    ///     Crew major win. The entirety of the Gang has been killed or arrested.
    /// </summary>
    CrewMajor
}
