using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiia.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaeConsumedComponent : Component
{
    [DataField]
    public int TimesConsumed;
}
