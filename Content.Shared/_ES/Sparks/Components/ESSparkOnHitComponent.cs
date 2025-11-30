using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity that sparks when damaged by something
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
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
    /// Chance for sparks to occur
    /// </summary>
    [DataField]
    public float Prob = 1f;

    /// <summary>
    /// Minimum time inbetween sparks occuring from hits.
    /// Used to reduce spark spam.
    /// </summary>
    [DataField]
    public TimeSpan SparkDelay = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The last time that a spark occured.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastSparkTime;

    /// <summary>
    /// Spark prototypes
    /// </summary>
    [DataField]
    public EntProtoId SparkPrototype = ESSparksSystem.DefaultSparks;
}
