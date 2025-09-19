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
    /// The machine linking port for the analyzer
    /// </summary>
    [DataField("linkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string LinkingPort = "ArtifactAnalyzerSenderAdvancedNodeScanner";

    /// <summary>
    /// How far away from the analyzer pad can the advanced node scanner get before it no longer scans. Infinite if negative
    /// </summary>
    [DataField(required: true)]
    public int MaxDistanceFromAnalyzerPad;

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
    bool activated)
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
}

[Serializable]
public struct AdvancedNodeScannerArtifactData(
    int currentNodeId,
    HashSet<int> knownNodeIds,
    List<AdvancedNodeScannerNodeData> nodes
)
{
    /// <summary>
    /// Holds all the advanced node scanner data about artifact, mainly a container for all the nodes
    /// </summary>
    public int CurrentNodeId = currentNodeId;
    public HashSet<int> KnownNodeIds = knownNodeIds;
    public List<AdvancedNodeScannerNodeData> Nodes = nodes;
}