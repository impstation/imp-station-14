namespace Content.Server._Impstation.CrystalMass;

/// <summary>
/// Component for crystal mass entities that need their tile cleared <see cref="CrystalMassComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class ActiveTileClearCrystalMassComponent : Component
{
    // Used to do less lookups.

    /// <summary>
    /// If the crystal mass is a light source
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsLight;

    /// <summary>
    /// pointlight radius after clearing tile
    /// </summary>
    [DataField]
    public float LightRadius = 10f;

    /// <summary>
    /// pointlight energy after clearing tile
    /// </summary>
    [DataField]
    public float LightEnergy = 2f;

    /// <summary>
    /// pointlight color after clearing tile
    /// </summary>
    [DataField]
    public Color LightColor = Color.FromHex("#FBFF23");
}
