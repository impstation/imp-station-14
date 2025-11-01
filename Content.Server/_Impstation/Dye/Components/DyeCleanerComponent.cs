using Microsoft.EntityFrameworkCore;

namespace Content.Server._Impstation.Dye.Components;

[RegisterComponent]
/// <summary>
/// Allows an entity with this component to remove dye from dyed items it is washed with.
/// </summary>
public sealed partial class DyeCleanerComponent : Component
{
    [DataField]
    public bool DeleteOnUse = false;
}
