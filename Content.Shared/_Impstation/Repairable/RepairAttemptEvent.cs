using Content.Shared.Repairable;

namespace Content.Shared._Impstation.Repairable;

/// <summary>
/// Raised before a repair doafter begins.
/// Can be cancelled by other systems to prevent repairing.
/// </summary>
/// <param name="Repairer">The entity performing the repair. May be the same as <c>Target</c> if it is a self repair.</param>
/// <param name="Target">The entity being repaired.</param>
/// <param name="RepairableComponent">The component attached to <c>Target</c> associated with the repair.</param>
/// <param name="BaseDelay">The base doafter time for the repair.</param>
[ByRefEvent]
public record struct RepairAttemptEvent(EntityUid Repairer, EntityUid Target, RepairableComponent RepairableComponent, float BaseDelay)
{
    /// <summary>
    /// Set this to true to cancel the doafter.
    /// </summary>
    public bool Cancelled = false;

    /// <summary>
    /// The entity performing the repair.
    /// </summary>
    public readonly EntityUid Repairer = Repairer;

    /// <summary>
    /// The entity receving the repair
    /// </summary>
    public readonly EntityUid Target = Target;

    /// <summary>
    /// The component attached to Target that allows for the repair.
    /// </summary>
    public readonly RepairableComponent Repairable = RepairableComponent;

    /// <summary>
    /// The doafter time for the repair.
    /// </summary>
    public readonly float BaseDelay = BaseDelay;

    /// <summary>
    /// One-time adjustment to the doafter delay.
    /// </summary>
    public float AdditionalDelay = 0f;
}
