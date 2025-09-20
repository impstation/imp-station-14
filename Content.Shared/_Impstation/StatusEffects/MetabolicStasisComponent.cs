namespace Content.Shared._Impstation.StatusEffects;

[RegisterComponent]
public sealed partial class MetabolicStasisStatusEffectComponent : Component
{
    /// <summary>
    /// Higher number = slower metabolic rate.
    /// </summary>
    [DataField]
    public float StasisCoefficient = 100f;
}
