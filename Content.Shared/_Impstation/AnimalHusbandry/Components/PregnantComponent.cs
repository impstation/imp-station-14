using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
///     Finally.
/// </summary>
[RegisterComponent]
public sealed partial class PregnantComponent : Component
{
    /// <summary>
    ///     The entity to spawn when gestation is finished.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId EntityToSpawn;

    /// <summary>
    ///     The time that this pregnancy ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime = TimeSpan.Zero;
}
