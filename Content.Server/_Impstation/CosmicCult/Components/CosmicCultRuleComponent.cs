using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.CosmicCult.Components;

/// <summary>
/// Component for the CosmicCultRuleSystem that should store gameplay info.
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCultRuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public List<Entity<CosmicCultComponent>> Cultists = new();
}

// CosmicCultRuleComponent

public enum CosmicWinCondition : byte
{
    Win,
    MinorWin,
    Failure
}

public enum CosmicCultTier : byte
{
    Tier1,
    Tier2,
    Tier3
}
