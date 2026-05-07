using System;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Configuration for the Slasher effigy placement action.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherPlaceEffigyActionComponent : Component
{
    /// <summary>
    /// Effigy entity prototype spawned when placement completes successfully.
    /// </summary>
    [DataField]
    public EntProtoId EffigyPrototype { get; set; } = "SlasherEffigy";

    /// <summary>
    /// Maximum distance between the user and the chosen placement tile.
    /// </summary>
    [DataField]
    public float PlacementRange { get; set; } = 3f;

    /// <summary>
    /// Channel time required before the effigy is placed.
    /// </summary>
    [DataField]
    public TimeSpan PlacementDoAfterDelay { get; set; } = TimeSpan.FromSeconds(5);
}
