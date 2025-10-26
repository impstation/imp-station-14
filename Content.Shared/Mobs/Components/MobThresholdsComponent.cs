using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed partial class MobThresholdsComponent : Component
{
    [DataField("thresholds", required: true)]
    public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts")]
    public bool TriggersAlerts = true;

    [DataField("currentThresholdState")]
    public MobState CurrentThresholdState;

    /// <summary>
    /// The health alert that should be displayed for player controlled entities.
    /// Used for alternate health alerts (silicons, for example)
    /// </summary>
    [DataField("stateAlertDict")]
    public Dictionary<MobState, ProtoId<AlertPrototype>> StateAlertDict = new()
    {
        {MobState.Alive, "HumanHealth"},
        {MobState.Critical, "HumanCrit"},
        {MobState.Dead, "HumanDead"},
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> HealthAlertCategory = "Health";

    /// <summary>
    /// Whether or not this entity should display damage overlays (robots don't feel pain, black out etc.)
    /// IMP: If you'd prefer to disable specific overlays, see <seealso cref="ShowBruteOverlay"/>,
    /// <seealso cref="ShowAirlossOverlay"/> and <seealso cref="ShowCritOverlay"/>.
    /// </summary>
    [DataField("showOverlays")]
    public bool ShowOverlays = true;

    /// <summary>
    /// IMP: Whether or not this entity should display the red vignette for brute damage
    /// </summary>
    [DataField]
    public bool ShowBruteOverlay = true;

    /// <summary>
    /// IMP: Whether or not this entity should display the dark vignette for brute damage
    /// </summary>
    [DataField]
    public bool ShowAirlossOverlay = true;

    /// <summary>
    /// IMP: Whether or not this entity should display the white vignette for the death threshold
    /// </summary>
    [DataField]
    public bool ShowCritOverlay = true;

    /// <summary>
    /// Whether or not this entity can be revived out of a dead state.
    /// </summary>
    [DataField("allowRevives")]
    public bool AllowRevives;
}

[Serializable, NetSerializable]
public sealed class MobThresholdsComponentState : ComponentState
{
    public Dictionary<FixedPoint2, MobState> UnsortedThresholds;

    public bool TriggersAlerts;

    public MobState CurrentThresholdState;

    public Dictionary<MobState, ProtoId<AlertPrototype>> StateAlertDict;

    public bool ShowOverlays;
    // imp add start
    public bool ShowBruteOverlay;
    public bool ShowAirlossOverlay;
    public bool ShowCritOverlay;
    // imp add end

    public bool AllowRevives;

    public MobThresholdsComponentState(Dictionary<FixedPoint2, MobState> unsortedThresholds,
        bool triggersAlerts,
        MobState currentThresholdState,
        Dictionary<MobState,
        ProtoId<AlertPrototype>> stateAlertDict,
        bool showOverlays,
        // imp add start
        bool showBruteOverlay,
        bool showAirlossOverlay,
        bool showCritOverlay,
        // imp add end
        bool allowRevives)
    {
        UnsortedThresholds = unsortedThresholds;
        TriggersAlerts = triggersAlerts;
        CurrentThresholdState = currentThresholdState;
        StateAlertDict = stateAlertDict;
        ShowOverlays = showOverlays;
        // imp add start
        ShowBruteOverlay = showBruteOverlay;
        ShowAirlossOverlay = showAirlossOverlay;
        ShowCritOverlay = showCritOverlay;
        // imp add end
        AllowRevives = allowRevives;
    }
}
