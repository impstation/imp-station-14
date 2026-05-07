using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GameTicking.Rules;


/// <summary>
///     Gamerule that stars & ends the resonance cascade event
/// </summary>
[RegisterComponent, Access(typeof(CascadeRuleSystem))]
public sealed partial class CascadeRuleComponent : Component
{
    /// <summary>
    /// The current stage of the resonance cascade event
    /// </summary>
    [DataField]
    public ResonanceCascadeStage Stage = ResonanceCascadeStage.Beginning;

    /// <summary>
    /// Time until the round is ended in seconds
    /// </summary>
    [DataField]
    public TimeSpan TimeUntilEndRound = TimeSpan.FromSeconds(180);

    [DataField]
    public ProtoId<ContentTileDefinition> CrystalMassPlating = "PlatingCrystalMass";
    [DataField]
    public EntProtoId CrystalBulbPrototype = "CrystalBulb";
    [DataField]
    public EntProtoId SingularityPrototype = "Singularity";
}

[Serializable]
public enum ResonanceCascadeStage : sbyte
{
    Beginning = 0,
    Middle = 1,
    End = 2,
}
