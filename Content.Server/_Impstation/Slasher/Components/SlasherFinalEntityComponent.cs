namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marks the Slasher final boss entity. The paired system gibs humanoid mobs on contact
/// using a sensor fixture, reusing the EventHorizon StartCollideEvent pattern.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherFinalEntityComponent : Component
{
    /// <summary>
    /// Fixture ID that fires the gib event on contact with humanoids.
    /// </summary>
    [DataField]
    public string GibFixtureId { get; set; } = "boss_sensor";

    [DataField]
    public float MinSpeed { get; set; } = .5f;

    [DataField]
    public float MaxSpeed { get; set; } = .5f;

    [DataField]
    public float ImpulseInterval { get; set; } = 0.25f;

    [DataField]
    public float MaxChaseRadius { get; set; } = 50f;

    [DataField]
    public float ChangeVectorMinInterval { get; set; } = 9999f;

    [DataField]
    public float ChangeVectorMaxInterval { get; set; } = 9999f;
}
