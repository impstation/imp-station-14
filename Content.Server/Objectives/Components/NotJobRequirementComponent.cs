using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
// using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype; it got really cranky about using a serializer with an array and idk what to do about that, so I hope that wasn't too important!

/// <summary>
/// Requires that the player not have a certain job to have this objective.
/// </summary>
[RegisterComponent, Access(typeof(NotJobRequirementSystem))]
public sealed partial class NotJobRequirementComponent : Component
{
    /// <summary>
    /// ID of the job to ban from having this objective.
    /// </summary>
    [DataField(required: true)] // imp edit
    public string[] Job; // imp edit, allows multiple jobs to be blacklisted
}
