using Content.Shared.Destructible.Thresholds;
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
    public TimeSpan DurationToRoundEnd = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Time until the round including curTime
    /// </summary>
    [DataField]
    public TimeSpan TimeUntilEndRound;

    /// <summary>
    /// Amount of singularities to spawn once the round ends
    /// </summary>
    [DataField]
    public MinMax MinMaxSinglarity = new(1, 3);

    /// <summary>
    /// Amount of crystal mass to spawn throughout the station
    /// </summary>
    [DataField]
    public MinMax MinMaxCrystalMassSpawn = new(2, 4);

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
