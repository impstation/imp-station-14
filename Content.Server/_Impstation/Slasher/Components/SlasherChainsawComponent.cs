namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Configures the Slasher's chainsaw combat, fuel bonus, and held-visual behavior.
/// </summary>
[RegisterComponent, Access(typeof(SlasherChainsawSystem))]
public sealed partial class SlasherChainsawComponent : Component
{
    /// <summary>
    /// Attack rate used while the chainsaw engine is off.
    /// </summary>
    [DataField]
    public float InactiveAttackRate { get; private set; } = 1f;

    /// <summary>
    /// Attack rate used while the chainsaw engine is running.
    /// </summary>
    [DataField]
    public float ActiveAttackRate { get; private set; } = 4f;

    /// <summary>
    /// Multiplicative damage bonus applied while the chainsaw is active and still has welding fuel.
    /// </summary>
    [DataField]
    public float FueledDamageMultiplier { get; private set; } = 1.5f;

    /// <summary>
    /// Name of the internal solution container that stores welding fuel.
    /// </summary>
    [DataField]
    public string FuelSolution { get; private set; } = "Welder";

    /// <summary>
    /// Reagent consumed to power the chainsaw's bonus damage.
    /// </summary>
    [DataField]
    public string FuelReagent { get; private set; } = "WeldingFuel";

    /// <summary>
    /// Welding fuel consumed per second while the chainsaw is active.
    /// </summary>
    [DataField]
    public float FuelConsumptionPerSecond { get; private set; } = 10f;

    /// <summary>
    /// Held-prefix used while the chainsaw occupies both hands.
    /// </summary>
    [DataField]
    public string HeldPrefix { get; private set; } = "wielded";

    /// <summary>
    /// Unspent fractional fuel drain carried between update ticks.
    /// </summary>
    public float FuelConsumptionRemainder { get; set; }
}