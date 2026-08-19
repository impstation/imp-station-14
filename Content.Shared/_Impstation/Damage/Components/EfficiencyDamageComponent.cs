using Content.Shared.Damage;
using Content.Shared.Damage.Components;

namespace Content.Shared._Impstation.Damage;

[RegisterComponent]
[Access(typeof(EfficiencyDamageSystem))]
public sealed partial class EfficiencyDamageComponent : Component
{
    /// <summary>
    /// Final multiplier to the calculated damage before application.
    /// </summary>
    /// TODO: This is kind of redundant since you can specify a damage amount in the Damage field.
    [DataField]
    public float DamageMultiplier = 1f;

    /// <summary>
    /// Cache for DamageableComponent.
    /// </summary>
    public DamageableComponent? DamageableComponentCache = null;

    // Thresholds
    /// <summary>
    /// The value at which damage begins. If efficiency drops below this value, damage will start to be applied.
    /// </summary>
    [DataField]
    public float MinimumNominalEfficiency = 50f;
    // Damage Values

    /// <summary>
    /// The kind and amount of damage dealt to the entity every tick (while running subnominally).
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The upper bound of damage scaling. When running at minimum efficiency, the damage dealt will be this value * Damage * DamageMultiplier.
    /// </summary>
    /// <remarks>
    /// Does not account for <see cref="DamageMultiplier"/>. E.g. if DamageMultipier == 2f, then the upper bound for damage is 2 times this value.
    /// </remarks>
    [DataField]
    public float MaxDamageScaling = 1f;
}
