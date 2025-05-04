using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Dye;

[RegisterComponent]
public sealed partial class DyedComponent : Component
{
    [DataField]
    public Color CurrentColor = default!;

    [DataField]
    public EntProtoId? OriginalEntity;
}
