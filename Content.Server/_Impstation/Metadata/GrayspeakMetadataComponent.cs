namespace Content.Server._Impstation.Metadata;

/// <summary>
/// Applies the grayspeak accent to an entity's name and/or description.
/// </summary>
[RegisterComponent]
public sealed partial class GrayspeakMetadataComponent : Component
{
    /// <summary>
    /// Whether to apply the accent to the entity's name.
    /// </summary>
    [DataField]
    public bool ChangeName = true;

    /// <summary>
    /// Whether to apply the accent to the entity's description.
    /// </summary>
    [DataField]
    public bool ChangeDescription = true;

    /// <summary>
    /// The chance to apply the accent to the name and/or description.
    /// </summary>
    [DataField]
    public float Chance = 0.1f; // 10% chance by default
}
