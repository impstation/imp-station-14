using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiia.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaConsumeActionComponent : Component
{
    [DataField]
    public EntityUid? ConsumeAction;

    [DataField]
    public string? ConsumeActionId = "ActionKodepiiaConsume";

    public List<SoundSpecifier?> SoundPool = new()
    {
        new SoundPathSpecifier("/Audio/Effects/gib1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib3.ogg"),
    };

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool CanGib = true;
}
