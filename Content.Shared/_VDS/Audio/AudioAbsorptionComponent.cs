namespace Content.Shared._VDS.Audio;

[RegisterComponent]
public sealed partial class AudioAbsorptionComponent : Component
{
    /// <summary>
    /// How much audio should be absorbed when an audio echo ray reaches this entity.
    /// </summary>
    [DataField]
    public float Absorption = 0f;

    /// <summary>
    /// If the audio ray will pass through this entity, or bounce off of it (like a wall) instead.
    /// </summary>
    [DataField]
    public bool ReflectRay = false;
}
