using Content.Server.Objectives.Systems;

namespace Content.Server._Impstation.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random traitor.
/// If there are no traitors
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class PickRandomTraitorComponent : Component { }
