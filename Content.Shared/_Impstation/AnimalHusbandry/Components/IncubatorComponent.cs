using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Exists purely for the ability for Incubators to keep track of what they're doing
/// </summary>
[RegisterComponent]
public sealed partial class EggIncubatorComponent : Component
{
    /// <summary>
    /// How long it'll take to finish incubating the egg.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan IncubationTime = TimeSpan.Zero;

    /// <summary>
    /// The current incubation time that has elapsed.
    /// </summary>
    /// <remarks>
    /// Once this surpasses <see cref="IncubationTime"/>, the egg will hatch.
    /// </remarks>
    [DataField]
    public TimeSpan CurrentIncubationTime = TimeSpan.Zero;

    /// <summary>
    /// How often this incubator will update its incubation time.
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1.0f);

    /// <summary>
    /// The next time this incubator will update.
    /// </summary>
    [DataField]
    public TimeSpan LastUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// Egg we are currently incubating
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<IncubationComponent>? CurrentlyIncubated;

    /// <summary>
    /// Used for tracking visuals
    /// </summary>
    public IncubatorStatus Status;
}

[Serializable, NetSerializable]
public enum IncubatorVisualizerLayers : byte
{
    Status
}

[Serializable, NetSerializable]
public enum IncubatorStatus : byte
{
    Active,
    Inactive
}
