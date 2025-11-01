using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
public sealed partial class AdvancedNodeScannerComponent : Component
{
    /// <summary>
    /// The analyzer entity the advanced node scanner is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField]
    public NetEntity? AnalyzerEntity;

    /// <summary>
    /// The machine linking port for the advanced node scanner
    /// </summary>
    [DataField("LinkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string AdvancedNodeScannerLinkingPort = "AdvancedNodeScannerSender";

    /// <summary>
    /// Natural artifact visibility increase on analysis console graph
    /// +1 by default
    /// </summary>
    [DataField]
    public int NaturalNodeGraphVisibilityModifier = 1;
}
