using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CombatModeSprint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatModeSprintComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SprintCoefficient { get; set; } = 1.5f;

    [DataField, AutoNetworkedField]
    public bool DoImpactDamage { get; set; }

    [DataField, AutoNetworkedField]
    public LocId? BeginCombatMessage { get; set; }
    [DataField, AutoNetworkedField]
    public LocId? EndCombatMessage { get; set; }

    /// <summary>
    /// Settings for impact damage, if applicable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinimumSpeed { get; set; } = 3f;
    [DataField, AutoNetworkedField]
    public float StunSeconds { get; set; } = 3f;
    [DataField, AutoNetworkedField]
    public float DamageCooldown { get; set; } = 2f;
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
