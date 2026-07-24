using Robust.Shared.Serialization;

namespace Content.Shared.Gibbing.Events;



/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibType">What type of gibbing is occuring</param>
/// <param name="AllowedContainers">Containers we are allow to gib</param>
/// <param name="ExcludedContainers">Containers we are allow not allowed to gib</param>
[ByRefEvent] public record struct AttemptEntityContentsGibEvent(
    EntityUid Target,
    GibContentsOption GibType,
    List<string>? AllowedContainers,
    List<string>? ExcludedContainers
    );

/// <summary>
/// Raised just before gibbing to allow systems to cancel the gib.
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="Cancelled">Set to true to prevent the gibbing from happening</param>
[ByRefEvent]
// imp: separate cancel event so listeners can veto gibbing without rewriting other gib parameters.
public struct AttemptEntityGibCancelEvent
{
    public EntityUid Target;
    public bool Cancelled;

    public AttemptEntityGibCancelEvent(EntityUid target)
    {
        Target = target;
        Cancelled = false;
    }
}

/// <summary>
/// Called just before we actually gib the target entity, allows modification of gibbing parameters.
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibletCount">how many giblets to spawn</param>
/// <param name="GibType">What type of gibbing is occuring</param>
/// <param name="Cancelled">Set to true to prevent the gibbing from happening</param>
[ByRefEvent]
// imp: mutable gib event supports both cancellation and runtime adjustment of gib parameters.
public struct AttemptEntityGibEvent
{
    public EntityUid Target;
    public int GibletCount;
    public GibType GibType;
    public bool Cancelled;

    public AttemptEntityGibEvent(EntityUid target, int gibletCount, GibType gibType)
    {
        Target = target;
        GibletCount = gibletCount;
        GibType = gibType;
        Cancelled = false;
    }
}

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="DroppedEntities">Any entities that are spilled out (if any)</param>
[ByRefEvent] public record struct EntityGibbedEvent(EntityUid Target, List<EntityUid> DroppedEntities);

[Serializable, NetSerializable]
public enum GibType : byte
{
    Skip,
    Drop,
    Gib,
}

public enum GibContentsOption : byte
{
    Skip,
    Drop,
    Gib
}
