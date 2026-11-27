using Content.Shared.Damage;

namespace Content.Server._Impstation.Heretic.Ritual;

[RegisterComponent]
[Virtual]
public partial class SacrificeBehaviorComponent : Component
{
    /// <summary>
    /// Minimum amount of corpses.
    /// </summary>
    [DataField] public float Min { get; set; } = 1;

    /// <summary>
    /// Maximum amount of corpses.
    /// </summary>
    [DataField] public float Max { get; set; } = 1;

    /// <summary>
    /// Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float SacrificePoints { get; set; } = 2f;

    /// <summary>
    /// Points gained on sacrificing a command member.
    /// </summary>
    [DataField] public float CommandSacrificePoints { get; set; } = 3f;

    /// <summary>
    /// The type of damage to do to victims who aren't already dead.
    /// </summary>
    [DataField]
    public DamageSpecifier SacDamage { get; set; } = new()
    {
        DamageDict = new()
        {
            {"Asphyxiation", 100},
        }
    };
}
