namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Attached after rift transit to prevent immediate bounce-back loops.
/// </summary>
[RegisterComponent, Access(typeof(SlasherRiftTeleportSystem))]
public sealed partial class SlasherRiftTeleportTimeoutComponent : Component
{
    /// <summary>
    /// Rift the entity most recently entered; used to prevent immediate bounce-back loops.
    /// </summary>
    [ViewVariables]
    public EntityUid? EnteredRift;
}
