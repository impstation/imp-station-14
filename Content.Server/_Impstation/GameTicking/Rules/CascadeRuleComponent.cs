using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GameTicking.Rules;


/// <summary>
///     Gamerule that stars & ends the resonance cascade event
/// </summary>
[RegisterComponent, Access(typeof(CascadeRuleSystem))]
public sealed partial class CascadeRuleComponent : Component
{
    /// <summary>
    /// Time until the round is ended in seconds
    /// </summary>
    [DataField]
    public float TimeUntilEndRound = 120f;

    /// <summary>
    /// Prototype that is spawned in
    /// </summary>
    [DataField]
    public EntProtoId CrystalMassSpawnPrototype = "CrystalMass";

    /// <summary>
    /// Prototype that is spawned in
    /// </summary>
    [DataField]
    public EntProtoId CrystalMassTileSpawnPrototype = "PlatingCrystalMass";
}
