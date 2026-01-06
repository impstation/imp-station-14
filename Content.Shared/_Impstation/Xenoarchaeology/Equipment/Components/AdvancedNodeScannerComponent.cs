using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
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
    /// How often the advanced node scanner will check for changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DataUpdateInterval = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Next update tick gametime.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// For currently unlocking artifact - list of node incides triggered
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<int> TriggeredNodeIndexes = new();

    /// <summary>
    /// For currently unlocking artifact - list of node incides triggered as of last update
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<int> PreviousTriggeredNodeIndexes = new();

    /// <summary>
    /// Historic data for previous unlocking attempts per artifact
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, List<UnlockSession>> UnlockHistories = new();
}

[Serializable]
public struct UnlockSession(
    TimeSpan startTime,
    TimeSpan? endTime,
    HashSet<int> triggeredNodeIndexes,
    Entity<XenoArtifactNodeComponent>? unlockedNode)
{
    /// <summary>
    /// Stored data about an unlocking session
    /// </summary>
    public TimeSpan StartTime = startTime;
    public TimeSpan? EndTime = endTime;
    public HashSet<int> TriggeredNodeIndexes = triggeredNodeIndexes;
    public Entity<XenoArtifactNodeComponent>? UnlockedNode = unlockedNode;
}
