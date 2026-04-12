namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Marker component for the vent corpse effigy pulse rule.
/// Spawn configuration is provided via a sibling <see cref="Content.Server._Impstation.SpawnCrewCorpses.SpawnCrewCorpseComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleVentCorpseSystem))]
public sealed partial class SlasherGameRuleVentCorpseComponent : Component;
