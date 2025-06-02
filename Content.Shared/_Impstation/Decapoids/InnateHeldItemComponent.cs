using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Decapoids;

/// <summary>
/// Added to a hand body part. Will try to spawn a held item when the hand gets attached to the body.
/// </summary>
[RegisterComponent]
public sealed partial class InnateHeldItemComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId<ItemComponent> ItemPrototype;
}
