using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Raised when the Slasher dark-step channel do-after completes or is cancelled.
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

    /// <summary>
    /// Implements SlasherDarkStepDoAfterEvent logic.
    /// </summary>
    private SlasherDarkStepDoAfterEvent()
    {
    }

    /// <summary>
    /// Creates a dark-step do-after event with the target destination and temporary marker.
    /// </summary>
    /// <param name="destination">Destination to teleport to on success.</param>
    /// <param name="rift">Temporary marker entity spawned for the channel.</param>
    public SlasherDarkStepDoAfterEvent(NetCoordinates destination, NetEntity? rift)
    {
        Destination = destination;
        Rift = rift;
    }

    /// <summary>
    /// Creates a copy of this event because it carries payload fields.
    /// Marker-style do-after events can often return <c>this</c>, but this one stores destination/rift data.
    /// </summary>
    public override DoAfterEvent Clone() => new SlasherDarkStepDoAfterEvent(Destination, Rift);
}
