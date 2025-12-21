using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// The console that is used for artifact analysis
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AnalysisConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AnalyzerEntity;

    /// <summary>
    /// #IMP The advanced node scanner entity the console is linked (via analyzer relay).
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public NetEntity? AdvancedNodeScanner;

    [DataField]
    public SoundSpecifier? ScanFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    /// <summary>
    /// The sound played when an artifact has points extracted.
    /// </summary>e
    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Effects/radpulse11.ogg")
    {
        Params = new AudioParams
        {
            Volume = 4,
        }
    };

    /// <summary>
    /// The machine linking port for linking the console with the analyzer.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "ArtifactAnalyzerSender";

    /// <summary>
    ///     Imp edit. The direction the up/down depth bias is going.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BiasDirection BiasDirection = BiasDirection.Shallow;
}

[Serializable, NetSerializable]
public enum ArtifactAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleExtractButtonPressedMessage : BoundUserInterfaceMessage;

// imp edit start
[Serializable, NetSerializable]
public sealed class AnalysisConsoleShallowBiasButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AnalysisConsoleDeepRandomBiasButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AnalysisConsoleDeepLeftBiasButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AnalysisConsoleDeepRightBiasButtonPressedMessage : BoundUserInterfaceMessage;

public enum BiasDirection : byte
{
    Shallow, //Towards depth 0
    DeepRandom, //Away from depth 0, random
    DeepLeft, // Away from depth 0, prioritizing left on graph
    DeepRight // Away from depht 0, prioritizing right on graph
}
// imp edit end
