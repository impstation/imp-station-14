using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Dark-step action config.
/// </summary>
[RegisterComponent]
public sealed partial class LightLevelTeleportActionComponent : Component
{
    /// <summary>
    /// Channel duration, in seconds, before teleport resolves.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChannelTime { get; set; } = 2.5f;

    /// <summary>
    /// Maximum luminance that still allows teleport.
    /// The server checks luminance <= threshold.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AmbientDarkThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Temporary marker spawned while channeling.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId MarkerPrototype { get; set; } = "SlasherDarkTeleportMarker";
}
