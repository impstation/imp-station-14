using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.AttackRevenge;

/// <summary>
///     Component which stores a list of keys based on whichever entities with
///     <see cref="AttackRevengeComponent"/> this entity has attacked.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AttackMemoryComponent : Component
{
    /// <summary>
    ///     List of keys obtained from having attacked entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Keys = [];
}
