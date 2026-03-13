using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Component used by the IncubationSystem to track what comes out, how long it takes to do so and other things
/// </summary>
[RegisterComponent]
public sealed partial class IncubationComponent : Component
{
    /// <summary>
    /// How long this egg incubates for
    /// </summary>
    [DataField("incubationTime")]
    public TimeSpan IncubationTime = TimeSpan.FromSeconds(90);

    /// <summary>
    /// What comes out when the incubation is done?
    /// </summary>
    [DataField("incubatedResult"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId IncubatedResult;

}
