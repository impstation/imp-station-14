using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Pleebnar.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PleebnarGibActionComponent : Component
{
    [DataField]
    public EntityUid? gibAction;

    [DataField]
    public string? gibActionId = "ActionPleebnarGib";
}
