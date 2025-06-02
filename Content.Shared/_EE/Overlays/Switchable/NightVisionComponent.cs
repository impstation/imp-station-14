using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._EE.Overlays.Switchable;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : SwitchableOverlayComponent
{
    public override string? ToggleAction { get; set; } = "ToggleNightVision";

    public override Color Color { get; set; } = Color.FromHex("#98FB98");

}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
