using Content.Shared.FixedPoint;

namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Runtime healing state applied to victims currently hooked on a Slasher meathook.
/// Added on hook insertion and removed when the victim is unhooked or moved off the expected spike.
/// </summary>
[RegisterComponent, Access(typeof(SlasherMeatHookSystem))]
public sealed partial class SlasherMeatHookHealingComponent : Component
{
    /// <summary>
    /// Hook entity currently responsible for this runtime healing state.
    /// Used to verify the victim is still attached to the expected hook before each tick.
    /// </summary>
    public EntityUid Hook { get; set; }

    /// <summary>
    /// Time between healing ticks while the victim remains hooked.
    /// </summary>
    public TimeSpan HealInterval { get; set; }

    /// <summary>
    /// Next world time when a healing tick should run.
    /// The meathook system uses this with its update loop as a simple scheduler.
    /// </summary>
    public TimeSpan NextHealTime { get; set; }

    /// <summary>
    /// Maximum damage to heal each tick before blood stabilization is applied.
    /// </summary>
    public FixedPoint2 HealPerTick { get; set; }

    /// <summary>
    /// Stop healing once the victim reaches this total damage floor.
    /// </summary>
    public FixedPoint2 TargetDamage { get; set; }

    /// <summary>
    /// Blood level to maintain while the victim is on the hook.
    /// </summary>
    public float TargetBloodLevel { get; set; }
}
