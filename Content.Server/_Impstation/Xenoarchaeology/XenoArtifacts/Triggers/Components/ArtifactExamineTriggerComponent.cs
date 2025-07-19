namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when the artifact is examined.
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactExamineTriggerComponent : Component
{
    /// <summary>
    ///     Does the artifact apply effects targeted at 'users' of the artifact at the examiner?
    /// </summary>
    [DataField("examineCountsAsInRange")]
    public bool ExamineCountsAsInRange = false;
}
