namespace Content.Shared._VDS.Audio;

[RegisterComponent]
public sealed partial class AcousticDataComponent : Component
{
    /// <summary>
    /// How much audio should be absorbed when an audio echo ray reaches this entity.
    /// Ranges from 0% to 100% are ideal.
    /// Scales with distance.
    /// </summary>
    [DataField]
    public float Absorption = 0f;

    /// <summary>
    /// If the audio ray will pass through this entity, or bounce off of it (like a wall) instead.
    /// </summary>
    [DataField]
    public bool ReflectRay = false;
}
