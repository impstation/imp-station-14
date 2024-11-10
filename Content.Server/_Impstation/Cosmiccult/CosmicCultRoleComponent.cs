using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are using the cosmic cult systems.
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCultRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// For cult leads, how many people you have converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint ConvertedCount = 0;
}
