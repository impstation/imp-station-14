namespace Content.Server._Impstation.CrystalMass;

/// <summary>
/// Handles spreading of crystal mass
/// </summary>
[RegisterComponent]
public sealed partial class CrystalMassComponent : Component
{
    /// <summary>
    /// Chance to spread whenever an edge spread is possible.
    /// </summary>
    [DataField]
    public float SpreadChance = 1f;

    /// <summary>
    /// number of sprite variations for crystal mass
    /// </summary>
    [DataField]
    public int SpriteVariants = 6;
}
