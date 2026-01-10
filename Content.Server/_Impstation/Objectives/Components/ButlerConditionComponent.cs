using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// For spawning a remote signaller on the TargetObjectiveSystem selected target
/// </summary>
[RegisterComponent, Access(typeof(ButlerConditionSystem))]
public sealed partial class ButlerConditionComponent : Component
{
    /// <summary>
    /// Autolinked butler remote signaller to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Signaller = "RemoteSignallerButler";
}
