using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Pleebnar.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PleebnarComponent : Component
{
    [DataField]
    public EntityUid? pleebnarUid;


}
