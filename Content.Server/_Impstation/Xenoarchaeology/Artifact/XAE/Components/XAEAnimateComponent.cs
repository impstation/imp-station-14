namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for recharging all nearby batteries when activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEAnimateSystem))]
public sealed partial class XAEAnimateComponent : Component
{
    /// <summary>
    /// Distance from the artifact to animate objects
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 6f;

    /// <summary>
    /// Duration of the animation.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 15f;

    /// <summary>
    /// Number of objects to animate
    /// </summary>
    [DataField("count"), ViewVariables(VVAccess.ReadWrite)]
    public int Count = 1;
}
