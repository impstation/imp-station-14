using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.Cosmiccult.Components;

/// <summary>
/// Component for the CosmicCultRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(CosmicCultRuleSystem))]
public sealed partial class CosmicCultRuleComponent : Component
{
    /// <summary>
    /// When the round will if all the command are dead (Incase they are in space)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CommandCheck;

    /// <summary>
    /// The amount of time between each check for command check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The time it takes after the last head is killed for the shuttle to arrive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(5);
}

// CosmicCultRuleComponent
