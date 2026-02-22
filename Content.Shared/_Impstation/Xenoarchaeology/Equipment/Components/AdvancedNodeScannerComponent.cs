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
    public Dictionary<EntityUid, UnlockSession> ArtifactUnlockSessions = new();

    /// <summary>
    /// Historic data for previous unlocking attempts per artifact.
    /// Dictionary key is artifact entityuid as integer
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, List<UnlockSession>> UnlockHistories = new();
    #endregion
}

[Serializable, NetSerializable]
public struct NodeActivation(
    TimeSpan? activateTime,
    int index,
    EntityUid? node,
    string? identifier,
    string? trigger
    )
{
    public TimeSpan? ActivateTime = activateTime;
    public int Index = index;
    public EntityUid? Node = node;
    public string? Identifier = identifier;
    public string? Trigger = trigger;
}

[Serializable, NetSerializable]
public struct UnlockSession(
    EntityUid? artifact,
    string artifactName,
    TimeSpan startTime,
    TimeSpan? endTime,
    List<NodeActivation> activatedNodes,
    bool artifexiumApplied,
    EntityUid? unlockedNode)
{
    /// <summary>
    /// Stored data about an unlocking session
    /// </summary>
    public EntityUid? Artifact = artifact;

    public string ArtifactName = artifactName;
    public TimeSpan StartTime = startTime;
    public TimeSpan? EndTime = endTime;
    public List<NodeActivation> ActivatedNodes = activatedNodes;
    public bool ArtifexiumApplied = artifexiumApplied;
    public EntityUid? UnlockedNode = unlockedNode;
}
