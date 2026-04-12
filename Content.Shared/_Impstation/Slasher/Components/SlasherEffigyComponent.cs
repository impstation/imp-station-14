using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Anomaly;
using Content.Shared.Eye;
using Content.Shared._Impstation.Slasher.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Shared state for a Slasher effigy entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SlasherEffigyComponent : Component
{
    /// <summary>
    /// Visibility mask layer used for hidden Slasher effigy rendering.
    /// Keep this value in sync with the effigy prototype's hidden visibility layer.
    /// </summary>
    public const int LayerMask = (int)VisibilityFlags.Slasher;

    [DataField, AutoNetworkedField]
    public bool Revealed { get; set; }

    [DataField, AutoNetworkedField]
    public int Insertions { get; set; }

    /// <summary>
    /// Particle type currently required to disrupt the effigy during reveal windows.
    /// This value rerolls whenever the effigy is revealed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType RequiredContainmentParticle { get; set; } = AnomalousParticleType.Delta;

    /// <summary>
    /// Structural damage applied to the effigy by a matching anomalous particle hit while revealed.
    /// </summary>
    [DataField]
    public float MatchingParticleDamage { get; set; } = 10f;

    /// <summary>How long the do-after takes when inserting a soul fragment.</summary>
    [DataField]
    public TimeSpan InsertDoAfterDuration { get; set; } = TimeSpan.FromSeconds(10f);

    /// <summary>How long the effigy stays revealed (vulnerable) after receiving a fragment.</summary>
    [DataField]
    public TimeSpan VulnerabilityDuration { get; set; } = TimeSpan.FromSeconds(30f);

    /// <summary>Damage healed on the inserting Slasher per successful fragment insertion.</summary>
    [DataField]
    public float HealPerFragment { get; set; } = 50f;

    /// <summary>
    /// Total damage threshold at which all Slashers receive a warning that the effigy is under attack.
    /// </summary>
    [DataField]
    public float DamagedWarningThreshold { get; set; } = 200f;

    /// <summary>
    /// Delay before victory event processing after the final fragment insertion popup appears.
    /// </summary>
    [DataField]
    public TimeSpan VictorySequenceDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Number of fragments remaining to trigger the final phase
    /// (permanent reveal + stationwide Medium + forced Code White).
    /// Example: 2 means trigger when insertions reach (target - 2).
    /// </summary>
    [DataField]
    public int FinalPhaseFragmentsRemaining { get; set; } = 2;

    /// <summary>
    /// Damage types that can affect the effigy while it is revealed.
    /// Hidden effigies ignore all damage.
    /// </summary>
    [DataField]
    public HashSet<string> RevealedDamageTypes { get; set; } = new()
    {
        "Heat",
        "Structural",
    };

    /// <summary>Whether moving cancels the insert do-after.</summary>
    [DataField]
    public bool BreakOnMove { get; set; } = true;

    /// <summary>Whether hand-interacting cancels the insert do-after.</summary>
    [DataField]
    public bool BreakOnHandInteract { get; set; } = true;

    /// <summary>Server-only: the tick time after which the effigy re-hides. Not networked.</summary>
    public TimeSpan? VulnerableUntil { get; set; }

    /// <summary>
    /// Server-only: when true, the effigy is permanently revealed and should never auto-hide.
    /// Used once the ritual reaches the final two-fragments phase.
    /// </summary>
    public bool PermanentlyRevealed { get; set; }

    // --- Pulse effect configuration ---

    /// <summary>Chance (0–1) that a stationwide pulse effect fires when the effigy re-hides.</summary>
    [DataField]
    public float PulseChance { get; set; } = 1f;

    /// <summary>
    /// Progress fraction required before hide pulses upgrade from one pulse to two.
    /// </summary>
    [DataField]
    public float DoublePulseProgressThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Reference to the pulse weights prototype that defines available pulse effects and their probabilities.
    /// </summary>
    [DataField]
    public ProtoId<PulseWeightsPrototype> PulseWeightsProto { get; set; } = "SlasherDefaultPulseWeights";

    /// <summary>Server-only: proto ID of the last triggered pulse effect; used to prevent immediate repeats.</summary>
    public string? LastPulseEffect { get; set; }
}

/// <summary>
/// Enumeration values for SlasherEffigyVisuals.
/// </summary>
[Serializable, NetSerializable]
public enum SlasherEffigyVisuals : byte
{
    Status,
}

/// <summary>
/// Enumeration values for SlasherEffigyStatus.
/// </summary>
[Serializable, NetSerializable]
public enum SlasherEffigyStatus : byte
{
    Hidden,
    Revealed,
}

/// <summary>
/// Defines a pulse effect and its relative weight for random selection.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PulseWeightEntry
{
    /// <summary>Proto ID of the pulse game rule to trigger.</summary>
    [DataField(required: true)]
    public string ProtoId { get; set; } = string.Empty;

    /// <summary>Relative weight (higher = more likely). Set to 0 to disable this pulse.</summary>
    [DataField]
    public int Weight { get; set; } = 20;

    /// <summary>
    /// Implements PulseWeightEntry logic.
    /// </summary>
    public PulseWeightEntry() { }

    /// <summary>
    /// Type definition for PulseWeightEntry.
    /// </summary>
    /// <param name="protoId">Parameter used by this method: protoId.</param>
    /// <param name="weight">Parameter used by this method: weight.</param>
    public PulseWeightEntry(string protoId, int weight)
    {
        ProtoId = protoId;
        Weight = weight;
    }
}
