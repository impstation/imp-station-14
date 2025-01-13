using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.CosmicCult.Components;

/// <summary>
/// Component for the CosmicCultRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(CosmicCultRuleSystem))]
public sealed partial class CosmicCultRuleComponent : Component
{
    public readonly List<EntityUid> CosmicCultMinds = new();
}

// CosmicCultRuleComponent
