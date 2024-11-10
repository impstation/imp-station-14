using Content.Shared.Roles;

namespace Content.Server._Impstation.Cosmiccult;

/// <summary>
///     Added to mind role entities to tag that they are a Revolutionary.
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCultRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// For headrevs, how many people you have converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint ConvertedCount = 0;
}
