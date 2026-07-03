using Content.Shared.Anomaly;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This is used for scanning anomalies and
/// displaying information about them in the ui
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalyScannerSystem))]
[NetworkedComponent]
public sealed partial class AnomalyScannerComponent : Component
{
    /// <summary>
    /// The entity that was last scanned by this scanner.
    /// Can be an anomaly or another scanner-compatible target.
    /// </summary>
    // imp: broaden scanner tracking to support non-anomaly scan targets.
    [ViewVariables]
    public EntityUid? ScannedEntity;

    /// <summary>
    /// Backward-compatible alias for anomaly scanner code paths that still refer to scanned anomaly.
    /// </summary>
    // imp: preserve legacy property access while the rest of the scanner code migrates to ScannedEntity.
    public EntityUid? ScannedAnomaly
    {
        get => ScannedEntity;
        set => ScannedEntity = value;
    }

    /// <summary>
    /// How long the scan takes
    /// </summary>
    [DataField]
    public float ScanDoAfterDuration = 5;

    /// <summary>
    /// The sound plays when the scan finished
    /// </summary>
    [DataField]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");
}
