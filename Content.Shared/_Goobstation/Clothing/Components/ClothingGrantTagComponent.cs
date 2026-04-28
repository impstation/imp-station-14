using Content.Shared.Tag; // harmony
using Robust.Shared.Prototypes; // harmony

namespace Content.Shared._Goobstation.Clothing.Components;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    [DataField("tag", required: true), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<TagPrototype> Tag = ""; // Harmony - change to protoid
 //this make stealth suit work
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;
}
