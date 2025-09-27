namespace Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

[RegisterComponent]
public sealed partial class OldAdvancedNodeScannerComponent : Component
{
    /// <summary>
    /// The analyzer entity the advanced node scanner is linked.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AnalyzerEntity;

    /// <summary>
    /// The machine linking port for the advanced node scanner
    /// </summary>
    [DataField("LinkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string AdvancedNodeScannerLinkingPort = "AdvancedNodeScannerSender";

    /// <summary>
    /// How far away from the analyzer pad can the advanced node scanner get before it no longer scans. Infinite if negative
    /// </summary>
    [DataField(required: true)]
    public float MaxDistanceFromAnalyzerPad;

    /// <summary>
    /// How much time since the last full advanced scan before we can fully scan again (prevents instant-scan spam)
    /// </summary>
    [DataField("minTimeBetweenFullAdvancedScans")]
    public TimeSpan MinTimeBetweenFullAdvancedScans = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Stored scanned artifacts and their nodes
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, AdvancedNodeScannerArtifactData> ScannedArtifactData = new Dictionary<EntityUid, AdvancedNodeScannerArtifactData>();
}

[Serializable]
public struct AdvancedNodeScannerNodeData(
    int nodeId,
    int depth,
    int? parentId,
    List<int> childIds,
    string trigger,
    string effect,
    bool activated,
    TimeSpan lastUpdated
    )
{
    /// <summary>
    /// stored data about an artifact node
    /// </summary>
    public int NodeId = nodeId;
    public int Depth = depth;
    public int? ParentId = parentId;
    public List<int> ChildIds = childIds;
    public string Trigger = trigger;
    public string Effect = effect;
    public bool Activated = activated;
    public TimeSpan LastUpdated = lastUpdated;
}

[Serializable]
public struct AdvancedNodeScannerArtifactData(
    int currentNodeId,
    TimeSpan currentNodeIdLastUpdated,
    HashSet<int> knownNodeIds,
    List<AdvancedNodeScannerNodeData> nodes
)
{
    /// <summary>
    /// Holds all the advanced node scanner data about artifact, mainly a container for all the nodes
    /// </summary>
    public int CurrentNodeId = currentNodeId;
    public TimeSpan CurrentNodeIdLastUpdated = currentNodeIdLastUpdated;
    public HashSet<int> KnownNodeIds = knownNodeIds;
    public List<AdvancedNodeScannerNodeData> Nodes = nodes;
}