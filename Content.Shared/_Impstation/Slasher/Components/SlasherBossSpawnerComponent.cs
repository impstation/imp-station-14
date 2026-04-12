namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Marks a map entity as the spawn point for the Slasher final boss.
/// The victory system queries these on the death-maze grid and replaces them with the boss entity.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherBossSpawnerComponent : Component { }
