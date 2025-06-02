using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Pleebnar.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PleebnarTelepathyActionComponent : Component
{
    [DataField]
    public EntityUid? TelepathyAction;

    [DataField]
    public string? TelepathyActionId = "ActionPleebnarTelepathy";

    [DataField]
    public SoundSpecifier? Announce = new SoundPathSpecifier("/Audio/_Impstation/Animals/pleebnar_weird.ogg")
    {
        Params = AudioParams.Default.WithVolume(4)
    };

    [DataField]
    public string? announceAudioPath = "/Audio/_Impstation/Animals/pleebnar_weird.ogg";
    [DataField]
    public EntityUid? VisionAction;

    [DataField]
    public string? VisionActionId = "ActionPleebnarVision";

    [DataField]
    public string? PleebnarVison;
    [DataField]
    public string? PleebnarVisonName;
}
