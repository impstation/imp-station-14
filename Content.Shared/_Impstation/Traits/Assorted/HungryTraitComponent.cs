using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// Stores hunger override values for the hungry trait
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class HungryTraitComponent : Component
{
    /// <summary>
    /// The rate of hunger stored before component is setup. Defaults to BaseDecayRate from HungerComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StoredHunger = 0.01666666666f;

    /// <summary>
    /// The rate of hunger stored before component is setup. Defaults to BaseDecayRate from HungerComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HungerLevel = 75.0f; //halfway between pekish and starving

    /// <summary>
    /// The rate of hunger applied on component startup.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungryRate = 0.15f;
}
