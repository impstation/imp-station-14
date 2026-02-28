using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.AttackRevenge;

/// <summary>
///     Component which cancels the attached entity's attack behaviour if the target has not attacked them previously.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttackRevengeComponent : Component
{
    /// <summary>
    ///     The key which this component will apply to the attacker entity's <see cref="AttackMemoryComponent"/>.
    /// </summary>
    [DataField(required: true)]
    public string Key;
}
