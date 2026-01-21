using Robust.Shared.Utility;

namespace Content.Server._Impstation.Borgs.FreeformLaws;

[RegisterComponent]
public sealed partial class FreeformLawEntryComponent : Component
{
    [DataField]
    public LocId VerbName = "silicon-law-ui-verb";

    [DataField]
    public SpriteSpecifier? VerbIcon = null;
}
