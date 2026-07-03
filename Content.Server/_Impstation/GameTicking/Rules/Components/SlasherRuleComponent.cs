using Content.Server._Impstation.Slasher;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.GameTicking.Rules.Components;

/// <summary>
/// Server-only Slasher rule state. This component is not networked.
/// </summary>
[RegisterComponent, Access(typeof(SlasherRuleSystem), typeof(SlasherEffigySystem), typeof(SlasherMeatHookSystem), typeof(SlasherDeathTeleportSystem), typeof(SlasherVictorySystem))]
public sealed partial class SlasherRuleComponent : Component
{
    /// <summary>
    /// Shuttle map used to spawn each slasher's private spawn area.
    /// </summary>
    [DataField("spawnShuttlePath", customTypeSerializer: typeof(ResPathSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public ResPath SpawnShuttlePath { get; set; } = new("/Maps/_Impstation/Nonstations/SlasherSpawnShuttle.yml");

    /// <summary>
    /// Current active effigy entity, if one exists.
    /// </summary>
    [DataField]
    public EntityUid? ActiveEffigy { get; set; }

    /// <summary>
    /// Tracks whether any slasher has placed an effigy this round.
    /// </summary>
    [DataField]
    public bool EffigyPlacedEver { get; set; }

    /// <summary>
    /// Tracks whether the crew has destroyed the active effigy.
    /// </summary>
    [DataField]
    public bool EffigyDestroyed { get; set; }

    /// <summary>
    /// Number of soul fragments inserted into the effigy so far.
    /// </summary>
    [DataField]
    public int FragmentInsertions { get; set; }

    /// <summary>
    /// Required fragment count for the current round.
    /// </summary>
    [DataField]
    public int TargetInsertions { get; set; }

    /// <summary>
    /// Minimum fragment goal when scaling by player count.
    /// </summary>
    [DataField]
    public int MinInsertions { get; set; } = 6;

    /// <summary>
    /// Maximum fragment goal when scaling by player count.
    /// </summary>
    [DataField]
    public int MaxInsertions { get; set; } = 10;

    /// <summary>
    /// Minimum player count used for target-insertion scaling.
    /// </summary>
    [DataField]
    public int MinPlayersForGoal { get; set; } = 15;

    /// <summary>
    /// Maximum player count used for target-insertion scaling.
    /// </summary>
    [DataField]
    public int MaxPlayersForGoal { get; set; } = 40;

    /// <summary>
    /// Set once the rule has triggered evacuation due to effigy failure conditions.
    /// </summary>
    [DataField]
    public bool EvacTriggered { get; set; }

    /// <summary>Total meathooks placed by all Slashers this round.</summary>
    [DataField]
    public int MeathookCount { get; set; }

    /// <summary>Set once the effigy is fully fed; prevents duplicate victory sequences and guards the death-shuttle path.</summary>
    [DataField]
    public bool VictoryTriggered { get; set; }

    /// <summary>
    /// Captured round-end outcome used for Slasher major/minor EOR messaging.
    /// </summary>
    [DataField]
    public SlasherRoundEndOutcome RoundEndOutcome { get; set; } = SlasherRoundEndOutcome.Unset;

    /// <summary>
    /// Set once the round enters the final two-fragments phase.
    /// Prevents repeatedly applying medium/alert-side effects on every insertion.
    /// </summary>
    [DataField]
    public bool FinalPhaseTriggered { get; set; }

    /// <summary>
    /// Combat sprint coefficient applied to Slashers when victory teleports them into the death maze.
    /// </summary>
    [DataField]
    public float DeathMazeSprintCoefficient { get; set; } = 1.75f;

    /// <summary>
    /// Delay before the round ends after Slasher victory sequence fires.
    /// </summary>
    [DataField]
    public TimeSpan VictoryRoundEndDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum random tile checks when falling back to random valid floor in the death maze.
    /// </summary>
    [DataField]
    public int DeathMazeSearchAttempts { get; set; } = 100;

    /// <summary>
    /// Minimum reachable valid tiles required from the center tile (0,0) for the maze to be accepted.
    /// </summary>
    [DataField]
    public int DeathMazeMinimumReachableTiles { get; set; } = 50;

    /// <summary>
    /// Minimum separation distance (in tiles) enforced when selecting marker spawns in batches.
    /// </summary>
    [DataField]
    public int DeathMazeSpawnMinSeparation { get; set; } = 4;

    /// <summary>
    /// Require a minimum number of reachable tiles with safe atmosphere.
    /// </summary>
    [DataField]
    public bool RequireDeathMazeAtmosphere { get; set; } = true;

    /// <summary>
    /// Minimum count of reachable tiles that must have safe atmosphere.
    /// </summary>
    [DataField]
    public int DeathMazeMinimumSafeAtmosTiles { get; set; } = 50;

    /// <summary>
    /// Require at least one powered light on the death-maze grid.
    /// </summary>
    [DataField]
    public bool RequireDeathMazePower { get; set; } = true;

    /// <summary>
    /// Minimum powered lights required on the death-maze grid.
    /// </summary>
    [DataField]
    public int DeathMazeMinimumPoweredLights { get; set; } = 1;

    /// <summary>
    /// Number of radial hallway sample lines cast from center when preferring spawn fallback tiles.
    /// Each line direction is snapped to the nearest 45 degrees.
    /// </summary>
    [DataField]
    public int DeathMazeSampleLineCount { get; set; } = 27;

    /// <summary>
    /// Maximum tile length per sampled fallback hallway line.
    /// </summary>
    [DataField]
    public int DeathMazeSampleLineLength { get; set; } = 66;

    /// <summary>
    /// Rift prototype spawned in the death maze for Slasher return portals.
    /// </summary>
    [DataField]
    public EntProtoId DeathMazePortalPrototype { get; set; } = "MidroundAntagTeleporter";

    /// <summary>
    /// Number of additional return portals to spawn on accessible non-center tiles.
    /// One portal is always spawned at the death-maze origin tile (0,0).
    /// </summary>
    [DataField]
    public int DeathMazeAdditionalPortalCount { get; set; } = 4;

    /// <summary>
    /// Delay between a Slasher entering the death maze and return portal spawn.
    /// </summary>
    [DataField]
    public TimeSpan DeathMazePortalSpawnDelay { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Lobby music collection used for Slasher victory round end.
    /// </summary>
    [DataField]
    public string VictoryLobbyMusicCollection { get; set; } = "SlasherVictoryRoundEnd";
}

/// <summary>
/// Round-end result categories used for Slasher EOR summary text.
/// </summary>
public enum SlasherRoundEndOutcome : byte
{
    Unset,
    SlasherMajor,
    CrewMinor,
    CrewMajor,
}
