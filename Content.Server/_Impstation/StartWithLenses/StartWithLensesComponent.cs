using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StartWithLenses;

[RegisterComponent]
public sealed partial class StartWithLensesComponent : Component
{
    [DataField]
    public EntProtoId LensPrototype = "PrescriptionLensStrong";
}
