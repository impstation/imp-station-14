using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._EE.Overlays.Switchable;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : SwitchableOverlayComponent
{
    public override string? ToggleAction { get; set; } = "ToggleNightVision";

    public override Color Color { get; set; } = Color.FromHex("#98FB98");

    /// <summary>
    /// imp. if true, uses DrawLighting instead of DrawShadows. Unfortunately this makes aliens naked, so I can only use it on drones.
    /// </summary>
    [DataField]
    public bool UseGoodLighting;
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
