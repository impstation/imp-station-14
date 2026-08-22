using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Monarch.Components;

[RegisterComponent]
public sealed partial class MonarchRayComponent : Component
{
    public SlotFlags EquippedFlag = SlotFlags.HEAD;

    public EntityUid? CurrentWearer = null;

    public int DamageToWearer = 300;
    public ProtoId<DamageTypePrototype> WearerDamageType = "Cellular";
}
