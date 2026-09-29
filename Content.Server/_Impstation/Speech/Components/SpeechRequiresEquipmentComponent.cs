using Content.Server._Impstation.Speech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server._Impstation.Speech.Components;

[RegisterComponent]
[Access(typeof(SpeechRequiresEquipmentSystem))]
public sealed partial class SpeechRequiresEquipmentComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist? Whitelist;

    [DataField]
    public LocId? FailMessage;
}
