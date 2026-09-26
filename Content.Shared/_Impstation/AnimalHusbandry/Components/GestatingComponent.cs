using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
///     Represents an entity that is currently "gestating" another entity, such as a pregnant animal
///     or an unhatched egg.
/// </summary>
/// <remarks>
///     Formerly known as IncubationComponent and PregnantComponent. RIP
/// </remarks>
[RegisterComponent]
public sealed partial class GestatingComponent : Component
{
    /// <summary>
    ///     The length of time this entity needs to gestate.
    /// </summary>
    [DataField]
    public TimeSpan GestationTime = TimeSpan.FromSeconds(90);

    /// <summary>
    ///     The currently-elapsed gestation progress. This can be halted by special circumstances.
    /// </summary>
    /// <remarks>
    ///     Once this surpasses <see cref="GestationTime"/>, then an <see cref="EntityToSpawn"/> will spawn.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CurrentGestationTime = TimeSpan.Zero;

    /// <summary>
    ///     The entity spawned after this gestation finishes.
    /// </summary>
    [DataField]
    public EntProtoId EntityToSpawn;

    /// <summary>
    ///     Whether or not this entity should be deleted once gestation finishes.
    /// </summary>
    [DataField]
    public bool DeleteSelfOnSpawn = false;
}

/// <summary>
///     Whether or not this entity is currently capable of gestation.
/// </summary>
/// <param name="Cancelled">True if this entity cannot gestate currently.</param>
[ByRefEvent]
public record struct IsGestatingEvent(bool Cancelled = false);

/// <summary>
///     Whether or not this entity is currently unable to gestate.
///     If this is the case, then gestation will stop immediately.
/// </summary>
/// <param name="Handled">True if this entity is unable to gestate currently.</param>
[ByRefEvent]
public record struct IsUnableToGestateEvent(bool Handled = false);
