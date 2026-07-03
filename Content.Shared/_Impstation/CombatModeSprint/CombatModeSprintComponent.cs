using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CombatModeSprint;

/// <summary>
/// Adds a configurable movement-speed and impact-damage profile while the entity is in combat mode.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatModeSprintComponent : Component
{
    /// <summary>
    /// Multiplier applied to movement speed while combat mode is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprintCoefficient { get; set; } = 1.5f;

    /// <summary>
    /// Whether high-speed collisions should use the configured combat-mode impact settings.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoImpactDamage { get; set; }

    /// <summary>
    /// Popup shown when the entity enters combat mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? BeginCombatMessage { get; set; }

    /// <summary>
    /// Popup shown when the entity exits combat mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? EndCombatMessage { get; set; }

    /// <summary>
    /// Settings for impact damage, if applicable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinimumSpeed { get; set; } = 3f;

    /// <summary>
    /// Stun duration applied when a combat-mode impact damage collision occurs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StunSeconds { get; set; } = 3f;

    /// <summary>
    /// Minimum delay between combat-mode impact damage collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageCooldown { get; set; } = 2f;

    /// <summary>
    /// Damage scaling used for combat-mode impact collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedDamage { get; set; } = 1f;


    /// <summary>
    /// Defaults to reset to for impact damage. I would personally rather derive these in the code, but this is how SkatesComponent does it.
    /// </summary>
    [ViewVariables]
    public float DefaultMinimumSpeed { get; set; } = 20f;
    [ViewVariables]
    public float DefaultStunSeconds { get; set; } = 1f;
    [ViewVariables]
    public float DefaultDamageCooldown { get; set; } = 2f;
    [ViewVariables]
    public float DefaultSpeedDamage { get; set; } = 0.5f;
}
