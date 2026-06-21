namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised during non-weightless movement events.
/// Primarily used for NPC waddle animation.
/// </summary>
[ByRefEvent]
public struct DuringMovementEvent(bool nonZeroMovement)
{
    public bool NonZeroMovement = nonZeroMovement;
}
