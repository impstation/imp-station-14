using Content.Server.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeviceLinking;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// A machine that is combined and linked to the <see cref="AnalysisConsoleComponent"/>
/// in order to analyze artifacts and extract points.
/// </summary>
[RegisterComponent]
public sealed partial class OldArtifactAnalyzerComponent : Component
{
    /// <summary>
    /// How long it takes to analyze an artifact
    /// </summary>
    [DataField("analysisDuration", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The corresponding console entity.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables]
    public EntityUid? Console;

    /// <summary>
    /// The corresponding advanced node scanner entity.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables]
    public EntityUid? AdvancedNodeScanner;

    /// <summary>
    /// The machine linking port for the advanced node scanner
    /// </summary>
    [DataField("advancedNodeScannerLinkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string AdvancedNodeScannerLinkingPort = "AdvancedNodeScannerSender";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ReadyToPrint = false;

    [DataField("scanFinishedSound")]
    public SoundSpecifier ScanFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    #region Analysis Data
    [DataField]
    public EntityUid? LastAnalyzedArtifact;

    [ViewVariables]
    public ArtifactNode? LastAnalyzedNode;

    [ViewVariables(VVAccess.ReadWrite)]
    public int? LastAnalyzerPointValue;
    #endregion
}
