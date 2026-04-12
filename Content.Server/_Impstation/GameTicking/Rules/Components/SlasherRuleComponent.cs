using Content.Server._Impstation.Slasher;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.GameTicking.Rules.Components;

/// <summary>
/// Server-only Slasher rule state. This component is not networked.
/// </summary>
[RegisterComponent, Access(typeof(SlasherRuleSystem), typeof(SlasherEffigySystem), typeof(SlasherMeatHookSystem), typeof(SlasherDeathTeleportSystem), typeof(SlasherVictorySystem))]
public sealed partial class SlasherRuleComponent : Component
{
	[DataField("spawnShuttlePath", customTypeSerializer: typeof(ResPathSerializer)), ViewVariables(VVAccess.ReadWrite)]
	public ResPath SpawnShuttlePath { get; set; } = new("Maps/Shuttles/escape_pod_small.yml");

	[DataField("deathMazePath", customTypeSerializer: typeof(ResPathSerializer)), ViewVariables(VVAccess.ReadWrite)]
	public ResPath DeathMazePath { get; set; } = new("Maps/Shuttles/emergency_box.yml");

	[DataField]
	public EntityUid? ActiveEffigy { get; set; }

	[DataField]
	public bool EffigyPlacedEver { get; set; }

	[DataField]
	public bool EffigyDestroyed { get; set; }

	[DataField]
	public int FragmentInsertions { get; set; }

	[DataField]
	public int TargetInsertions { get; set; }

	[DataField]
	public int MinInsertions { get; set; } = 6;

	[DataField]
	public int MaxInsertions { get; set; } = 10;

	[DataField]
	public int MinPlayersForGoal { get; set; } = 15;

	[DataField]
	public int MaxPlayersForGoal { get; set; } = 40;

	[DataField]
	public bool EvacTriggered { get; set; }

	/// <summary>Total meathooks placed by all Slashers this round.</summary>
	[DataField]
	public int MeathookCount { get; set; }

	/// <summary>Set once the effigy is fully fed; prevents duplicate victory sequences and guards the death-shuttle path.</summary>
	[DataField]
	public bool VictoryTriggered { get; set; }

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
	/// Maximum random tile checks when selecting a spawn tile on a slasher's private spawn shuttle.
	/// </summary>
	[DataField]
	public int SpawnShuttleSearchAttempts { get; set; } = 200;

	/// <summary>
	/// Maximum random tile checks when falling back to random valid floor in the death maze.
	/// </summary>
	[DataField]
	public int DeathMazeSearchAttempts { get; set; } = 100;
}
