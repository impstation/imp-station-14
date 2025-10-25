using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.EntityEffects.Effects;

/// <summary>
/// Status effect that reverses your controls. up is down, left is right.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DizzySystem))]
public sealed partial class DizzyComponent : Component
{
    [DataField]
    public bool Dizzy = false;
    /// <summary>
    /// The interval at which this component updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Variable that stores the amount of status time added.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StatusTime;

    /// <summary>
    /// Amount of time remaining until the component shuts down.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeRemaining;

    [ViewVariables, AutoNetworkedField]
    public float SprintSpeedModifier = -1;

    [ViewVariables, AutoNetworkedField]
    public float WalkSpeedModifier = -1;
}
