using Content.Shared.Body.Events;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// Used for the Hemorrhaging trait. Modifies the amount of blood lost by an entity when it is bleeding.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HemorrhageComponent : Component
{
    /// <summary>
    /// Multiplier applied to the amount of blood lost during a <see cref="BleedModifierEvent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedAmountCoefficient = 1.4f;
}
