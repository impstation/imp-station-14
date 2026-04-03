using Robust.Shared.Audio;

namespace Content.Server._CD.StationEvents.Components;

/// <summary>
/// Gamerule component to notify a random synth player when started.
/// </summary>
[RegisterComponent]
public sealed partial class SynthStormRuleComponent : Component
{
    [DataField]
    public SoundSpecifier? SynthStormSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");
}
