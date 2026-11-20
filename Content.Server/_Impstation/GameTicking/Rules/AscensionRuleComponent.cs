namespace Content.Server._Impstation.GameTicking.Rules;


/// <summary>
///     Gamerule that starts various effects on a heretic ascension
/// </summary>
[RegisterComponent, Access(typeof(AscensionRuleSystem))]
public sealed partial class AscensionRuleComponent : Component
{
    /// <summary>
    /// A delay between when the rule the is started and when the cobalt things run. This is so sfx don't overlap
    /// </summary>
    [DataField]
    public TimeSpan DelayForCobaltEffects = TimeSpan.FromSeconds(15);

    /// <summary>
    /// percentage of lights that will flicker upon ascension
    /// </summary>
    [DataField]
    public float LightFlickerChance = 0.33f;

    public TimeSpan TimeForCobaltEffects;

    public bool CobaltEffectsTriggered = false;

    public EntityUid? Ascendant = null;
}
