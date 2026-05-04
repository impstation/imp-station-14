using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
///     An entity that can be incubated to produce another entity, such as a mob.
///     For example: a chicken egg.
/// </summary>
[RegisterComponent]
public sealed partial class IncubationComponent : Component
{
    /// <summary>
    /// How long this egg incubates for
    /// </summary>
    [DataField]
    public TimeSpan IncubationTime = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The current incubation time that has elapsed.
    /// </summary>
    /// <remarks>
    /// Once this surpasses <see cref="IncubationTime"/>, the egg will hatch.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CurrentIncubationTime = TimeSpan.Zero;

    /// <summary>
    /// What comes out when the incubation is done?
    /// </summary>
    [DataField]
    public EntProtoId IncubatedResult;
}
