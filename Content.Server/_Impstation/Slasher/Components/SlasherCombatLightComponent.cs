using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Grants and manages the Slasher's combat light implant lifecycle.
/// </summary>
[RegisterComponent, Access(typeof(SlasherCombatLightSystem))]
public sealed partial class SlasherCombatLightComponent : Component
{
    /// <summary>
    /// Implant prototype granted when this component starts up.
    /// </summary>
    [DataField]
    public EntProtoId ImplantPrototype { get; set; } = "SlasherLightImplant";

    /// <summary>
    /// Runtime reference to the implanted combat light entity for toggling and cleanup.
    /// Managed server-side by <see cref="SlasherCombatLightSystem"/> only.
    /// </summary>
    [ViewVariables]
    public EntityUid? ImplantUid { get; set; }
}
