namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent]
public sealed partial class MetabolicStasisComponent : Component
{
    /// <summary>
    /// Higher number = slower metabolic rate. 
    /// </summary>
    [DataField]
    public float StasisCoefficient = 100f;
}
