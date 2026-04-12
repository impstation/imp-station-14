using System;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Configuration for the Slasher effigy placement action.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherPlaceEffigyActionComponent : Component
{
    [DataField]
    public EntProtoId EffigyPrototype { get; set; } = "SlasherEffigy";

    [DataField]
    public float PlacementRange { get; set; } = 1.5f;

    [DataField]
    public TimeSpan PlacementDoAfterDelay { get; set; } = TimeSpan.FromSeconds(5);
}
