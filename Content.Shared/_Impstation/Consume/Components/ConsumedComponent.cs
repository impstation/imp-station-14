using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Consume.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ConsumedComponent : Component
{
    [DataField]
    public int TimesConsumed;
}
