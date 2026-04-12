using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.SpawnCrewCorpses;

/// <summary>
/// Configuration component for SpawnCrewCorpseSystem.
/// Add this alongside any game rule to control how corpses are spawned.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnCrewCorpseComponent : Component
{
    /// <summary>Minimum number of corpses to spawn.</summary>
    [DataField]
    public int MinSpawnCount { get; set; } = 1;

    /// <summary>Maximum number of corpses to spawn.</summary>
    [DataField]
    public int MaxSpawnCount { get; set; } = 1;

    /// <summary>Corpse prototype to spawn.</summary>
    [DataField]
    public EntProtoId CorpsePrototype { get; set; } = "SalvageHumanCorpse";

    /// <summary>Whether to clone visual appearance from a connected crew member.</summary>
    [DataField]
    public bool CloneAppearance { get; set; } = true;

    /// <summary>Whether each spawned corpse should use a distinct crew member's appearance.</summary>
    [DataField]
    public bool DistinctCrewPerBatch { get; set; } = true;

    /// <summary>Copy only the inner-clothing (jumpsuit) slot from the source crew member.</summary>
    [DataField]
    public bool CopyJumpsuitOnly { get; set; } = true;

    /// <summary>Display name assigned to every spawned corpse.</summary>
    [DataField]
    public string CorpseName { get; set; } = "unidentified corpse";
}
