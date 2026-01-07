using Content.Shared._EE.Overlays.Switchable;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;


namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SerpentFocusComponent : SwitchableOverlayComponent
{
    public override string? ToggleAction { get; set; } = "ActionHereticSerpentsFocus";

    public override Color Color { get; set; } = Color.FromHex("#800303");

    public override SoundSpecifier? ActivateSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/Weather/Wind_4_2.ogg");

    public override SoundSpecifier? DeactivateSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/Weather/Wind_2_1.ogg");
}

public sealed partial class ToggleSerpentFocusEvent : InstantActionEvent;
