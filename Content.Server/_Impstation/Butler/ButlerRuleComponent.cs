using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Choose the butler target and give them their remote.
/// </summary>
[RegisterComponent, Access(typeof(ButlerRuleSystem))]
public sealed partial class ButlerRuleComponent : Component
{
    /// <summary>
    /// Autolinked butler remote signaller to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Signaller = "RemoteSignallerButler";

    /// <summary>
    /// Entity of the butler's target player.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    ///     Entity of the butler player.
    /// </summary>
    [DataField]
    public EntityUid? Butler;
}
