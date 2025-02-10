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
    [DataField] public List<Entity<CosmicCultComponent>> Cultists = new();
    [DataField] public int CurrentTier = 0; // current cult tier
    [DataField] public int TotalCrew = 0; // total connected players
    [DataField] public int TotalCult = 0; // total cultists
    [DataField] public int TotalNotCult = 0; // total players that -aren't- cultists
    [DataField] public int CrewTillNextTier = 777; // players needed to be converted till next monument tier
    [DataField] public double PercentConverted = 0; // percentage of connected players that are cultists
    [DataField] public double Tier3Percent = 777; // 40 percent of connected players

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
