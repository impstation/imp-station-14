using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiiae.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaeScramblerComponent : Component
{
    [DataField]
    public EntityUid? ScramblerAction;
    [DataField]
    public string? ScramblerActionId = "ActionKodepiiaeScrambler";
    [DataField]
    public SoundSpecifier ScramblerSound = new SoundPathSpecifier("/Audio/_Impstation/Kodepiia/kodescramble/kodescramble.ogg");

}
