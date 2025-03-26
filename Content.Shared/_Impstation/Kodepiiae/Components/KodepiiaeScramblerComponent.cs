using Robust.Shared.Audio;
using Robust.Shared.GameStates;

<<<<<<<< HEAD:Content.Shared/_Impstation/Kodepiia/Components/KodepiiaeScramblerComponent.cs
namespace Content.Shared._Impstation.Kodepiia.Components;
========
namespace Content.Shared._Impstation.Kodepiiae.Components;
>>>>>>>> f763918bc7 (Finish up consuming and cleaned up code):Content.Shared/_Impstation/Kodepiiae/Components/KodepiiaeScramblerComponent.cs

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaScramblerComponent : Component
{
    [DataField]
    public EntityUid? ScramblerAction;
    [DataField]
    public string? ScramblerActionId = "ActionKodepiiaScrambler";
    [DataField]
    public SoundSpecifier ScramblerSound = new SoundPathSpecifier("/Audio/_Impstation/Kodepiia/kodescramble/kodescramble.ogg");

}
