using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AdvancedNodeScannerComponent : Component
{
    /// <summary>
    /// The SINGLE analyzer entity the advanced node scanner is linked to.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AnalyzerEntity;

    /// <summary>
    /// The machine linking port for the advanced node scanner
    /// </summary>
    [DataField("LinkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string AdvancedNodeScannerLinkingPort = "AdvancedNodeScannerSender";

    /// <summary>
    /// Natural artifact visibility increase on analysis console graph
    /// +1 by default
    /// </summary>
    [DataField, AutoNetworkedField]
    public int NaturalNodeGraphVisibilityModifier = 1;

    /// <summary>
    /// Point multiplier value
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PointMultiplier = 1f;

    #region Records
    /// <summary>
    /// Do we announce unlocking session changes using advertise system?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AnnounceUnlockingChanges = true;

    /// <summary>
    /// Currently monitored unlocking sessions
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, UnlockSession> ArtifactUnlockSessions = new();

    /// <summary>
    /// Historic data for previous unlocking attempts per artifact.
    /// Dictionary key is artifact NetEntity ID - to persist against deleted/crushed/sold artifact
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, List<UnlockSession>> UnlockHistories = new();
    #endregion
}

[Serializable, NetSerializable]
public struct NodeActivation(
    TimeSpan? activateTime,
    int index,
    int? node, // NetEntity ID - int to persist after artifact is crushed/sold/deleted
    string? identifier,
    string? trigger
    )
{
    public TimeSpan? ActivateTime = activateTime;
    public int Index = index;
    public int? Node = node; // NetEntity ID - int to persist after artifact is crushed/sold/deleted
    public string? Identifier = identifier;
    public string? Trigger = trigger;
}

[Serializable, NetSerializable]
public struct UnlockSession(
    int? artifact, // NetEntity ID - int to persist after artifact is crushed/sold/deleted
    string artifactName,
    TimeSpan startTime,
    TimeSpan? endTime,
    List<NodeActivation> activatedNodes,
    bool artifexiumApplied,
    int? unlockedNode) // NetEntity ID - int to persist after artifact is crushed/sold/deleted
{
    /// <summary>
    /// Stored data about an unlocking session
    /// </summary>
    public int? Artifact = artifact; // NetEntity ID - int to persist after artifact is crushed/sold/deleted

    public string ArtifactName = artifactName;
    public TimeSpan StartTime = startTime;
    public TimeSpan? EndTime = endTime;
    public List<NodeActivation> ActivatedNodes = activatedNodes;
    public bool ArtifexiumApplied = artifexiumApplied;
    public int? UnlockedNode = unlockedNode; // NetEntity ID - int to persist after artifact is crushed/sold/deleted
}
