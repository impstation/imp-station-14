using System;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Effigy placement config.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherPlaceEffigyActionComponent : Component
{
    /// <summary>
    /// Effigy prototype spawned on success.
    /// </summary>
    [DataField]
    public EntProtoId EffigyPrototype { get; set; } = "SlasherEffigy";

    /// <summary>
    /// Maximum distance between the user and the chosen placement tile.
    /// </summary>
    [DataField]
    public float PlacementRange { get; set; } = 3f;

    /// <summary>
    /// Channel time before placement.
    /// </summary>
    [DataField]
    public TimeSpan PlacementDoAfterDelay { get; set; } = TimeSpan.FromSeconds(5);
}
