using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Dark-step teleport do-after event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherDarkStepDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Destination to teleport to when the do-after succeeds.
    /// </summary>
    [DataField]
    public NetCoordinates Destination;

    /// <summary>
    /// Optional temporary marker entity spawned during channeling.
    /// </summary>
    [DataField]
    public NetEntity? Rift;

    private SlasherDarkStepDoAfterEvent()
    {
    }

    /// <summary>
    /// Create the dark-step do-after payload.
    /// </summary>
    /// <param name="destination">Destination to teleport to on success.</param>
    /// <param name="rift">Temporary marker entity spawned for the channel.</param>
    public SlasherDarkStepDoAfterEvent(NetCoordinates destination, NetEntity? rift)
    {
        Destination = destination;
        Rift = rift;
    }

    /// <summary>
    /// Clone the payload data.
    /// </summary>
    public override DoAfterEvent Clone() => new SlasherDarkStepDoAfterEvent(Destination, Rift);
}
