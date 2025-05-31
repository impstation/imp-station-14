using Content.Shared.Speech;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Pleebnar.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PleebnarTelepathyActionComponent : Component
{
    [DataField]
    public EntityUid? TelepathyAction;

    [DataField]
    public string? TelepathyActionId = "ActionPleebnarTelepathy";

    [DataField]
    public EntityUid? VisionAction;

    [DataField]
    public string? VisionActionId = "ActionPleebnarVision";

    [DataField]
    public string? PleebnarVison;
}
