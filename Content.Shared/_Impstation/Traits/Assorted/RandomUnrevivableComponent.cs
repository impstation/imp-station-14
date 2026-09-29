using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomUnrevivableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Chance = 0.5f;
}
