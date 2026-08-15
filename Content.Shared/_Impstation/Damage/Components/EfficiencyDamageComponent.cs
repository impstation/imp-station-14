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
    /// <remarks>
    /// TODO: This field is potentially edited by code, but also should exist for flat, user-defined multipliers.
    /// Should this be a DataField or not?
    /// </remarks>
    [DataField]
    public float DamageMultiplier = 1f;

    /// <summary>
    /// Cache for DamageableComponent.
    /// </summary>
    public DamageableComponent? _damageableComponent = null; //TODO: Make private. Doesn't seem to be accessible even with Access(typeof(eds))

    // Thresholds
    /// <summary>
    /// The value at which damage begins. If efficiency drops below this value, damage will start to be applied.
    /// </summary>
    [DataField]
    public float MinimumNominalEfficiency = 50f;
    // Damage Values
    [DataField]
    public DamageSpecifier Damage = default!;


    [DataField]
    public float MaxDamagePerTick = 1f;


    [DataField]
    public float MinDamagePerTick = 0f;
}
