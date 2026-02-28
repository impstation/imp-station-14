using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Objectives.Components;

/// <summary>
/// For spawning a remote signaller on the TargetObjectiveSystem selected target
/// </summary>
[RegisterComponent]
public sealed partial class ButlerConditionComponent : Component
{
    /// <summary>
    /// Box with autolinked butler remote signaller to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Package = "BoxButler";
    [DataField]
    public LocId ButlerSpawn = "butler-spawn";
}
