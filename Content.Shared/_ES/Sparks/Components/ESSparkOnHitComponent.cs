using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity that sparks when damaged by something
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSparkOnHitSystem))]
public sealed partial class ESSparkOnHitComponent : Component
{
    /// <summary>
    /// Amount of damage that needs to be dealt to cause sparks
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 1;

    /// <summary>
    /// Number of sparks
    /// </summary>
    [DataField]
    public int Count = 3;

    /// <summary>
    /// Spark prototypes
    /// </summary>
    [DataField]
    public EntProtoId SparkPrototype = ESSparksSystem.DefaultSparks;
}
