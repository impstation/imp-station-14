using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiiae.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaeConsumeActionComponent : Component
{
    [DataField]
    public EntityUid? ConsumeAction;
    [DataField]
    public string? ConsumeActionId = "ActionKodepiiaeConsume";

    public List<SoundSpecifier?> SoundPool = new()
    {
        new SoundPathSpecifier("/Audio/Effects/gib1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib3.ogg"),
    };
}
