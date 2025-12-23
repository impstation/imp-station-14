using Content.Server._Impstation.Traitor.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Traitor.Components;

/// <summary>
/// Rolls a defined chance to make the given mob the defined antag either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(RandomAntagChanceSystem))]
public sealed partial class RandomAntagChanceComponent : Component
{
    /// <summary>
    /// Sets the chance that the antag is rolled
    /// </summary>
    [DataField]
    public float Chance = 0.3f;

    /// <summary>
    /// Sets the antag type to be rolled for.
    /// </summary>
    /// <remarks>
    /// Value given must be a valid antag gamerule OR MindRolePrototype.
    /// </remarks>
    [DataField(required: true)]
    public EntProtoId AntagRole = "Traitor";

    /// <summary>
    /// The mind role given if the antag is not rolled.
    /// </summary>
    /// <remarks>
    /// Value given must be a valid antag gamerule OR MindRolePrototype.
    /// </remarks>
    [DataField(required: true)]
    public EntProtoId FallbackRole = "MindRoleGhostRoleNeutral";
}
