namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marks a rift entity as a collision-triggered teleporter handled by <see cref="SlasherRiftTeleportSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(SlasherRiftTeleportSystem))]
public sealed partial class SlasherRiftTeleportComponent : Component
{
	/// <summary>
	/// Fixture ID on the rift that should trigger teleport behavior.
	/// </summary>
	[DataField]
	public string PortalFixtureId { get; set; } = "portalFixture";

	/// <summary>
	/// Fixture ID on colliding entities that should be treated as projectiles.
	/// </summary>
	[DataField]
	public string ProjectileFixtureId { get; set; } = "projectile";
}
