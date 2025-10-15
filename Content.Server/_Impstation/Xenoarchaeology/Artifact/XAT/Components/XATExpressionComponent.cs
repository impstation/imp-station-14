namespace Content.Server.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated by having a nearby entity emote.
/// </summary>
[RegisterComponent, Access(typeof(XATExpressionSystem))]
public sealed partial class XATExpressionComponent : Component
{
    /// <summary>
    /// How close to the emote event the artifact has to be for it to trigger.
    /// </summary>
    [DataField]
    public float Range = 6f;
}