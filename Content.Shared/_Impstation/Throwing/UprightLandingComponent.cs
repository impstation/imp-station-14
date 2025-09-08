using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Throwing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UprightLandingComponent : Component
{
    /// <summary>
    /// Chance item lands upright.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Chance = 1.0f;
}
